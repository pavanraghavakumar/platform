using Hms.Web.Data;
using Hms.Web.Controllers;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Pharmacy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Hms.Tests.Pharmacy;

/// <summary>
/// Pharmacy rules (Requirements 9.1–9.9): stock-in adds to the stock and saves the batch,
/// stock-out subtracts or refuses with "Not enough stock available.", and the status rules
/// In Stock, Low Stock, Out of Stock and Expired follow the stock and the earliest expiry date.
/// Each test migrates its own throwaway SQLite file and asserts only on the rows it writes.
/// </summary>
public class PharmacyTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly HmsDbContext _db;
    private readonly PharmacyService _pharmacy;

    public PharmacyTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-pharmacy-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new HmsDbContext(options);
        _db.Database.Migrate();
        _pharmacy = new PharmacyService(_db);
    }

    private async Task<Medicine> AddMedicineAsync(int stock, int reorderLevel)
    {
        var medicine = new Medicine
        {
            Name = $"Test medicine {Guid.NewGuid():N}",
            UnitPrice = 12.50m,
            ReorderLevel = reorderLevel,
            StockQuantity = stock,
            Status = EntityStatus.MedicineInStock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();
        return medicine;
    }

    [Fact]
    public async Task Create_saves_a_new_medicine_with_in_stock_status()
    {
        var category = new MedicineCategory { Name = $"Category {Guid.NewGuid():N}" };
        var supplier = new Supplier { Name = $"Supplier {Guid.NewGuid():N}" };
        _db.AddRange(category, supplier);
        await _db.SaveChangesAsync();

        var controller = new MedicineController(_db, _pharmacy)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        var result = await controller.Create(new MedicineCreateViewModel
        {
            Name = "New medicine",
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            UnitPrice = 15.75m,
            ReorderLevel = 10,
            StockQuantity = 0
        });

        Assert.IsType<RedirectToActionResult>(result);
        var medicine = await _db.Medicines.AsNoTracking().SingleAsync(m => m.Name == "New medicine");
        Assert.Equal(category.Id, medicine.CategoryId);
        Assert.Equal(supplier.Id, medicine.SupplierId);
        Assert.Equal(15.75m, medicine.UnitPrice);
        Assert.Equal(10, medicine.ReorderLevel);
        Assert.Equal(0, medicine.StockQuantity);
        Assert.Equal(EntityStatus.MedicineInStock, medicine.Status);
    }

    [Fact]
    public async Task Create_rejects_negative_stock_and_does_not_save_the_medicine()
    {
        var controller = new MedicineController(_db, _pharmacy)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        var result = await controller.Create(new MedicineCreateViewModel
        {
            Name = "Invalid negative stock medicine",
            UnitPrice = 15.75m,
            ReorderLevel = 10,
            StockQuantity = -1
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(view.Model, Assert.IsType<MedicineCreateViewModel>(view.Model));
        var error = Assert.Single(controller.ModelState[nameof(MedicineCreateViewModel.StockQuantity)]!.Errors);
        Assert.Equal("Stock quantity cannot be negative.", error.ErrorMessage);
        Assert.False(await _db.Medicines.AsNoTracking()
            .AnyAsync(m => m.Name == "Invalid negative stock medicine"));
    }

    [Fact]
    public async Task Database_rejects_a_negative_medicine_stock_quantity()
    {
        var designTimeModel = _db.GetService<IDesignTimeModel>().Model;
        var medicine = designTimeModel.FindEntityType(typeof(Medicine));
        var constraint = Assert.Single(
            medicine!.GetCheckConstraints(),
            check => check.Name == "CK_Medicines_StockQuantity_NonNegative");

        Assert.Equal("StockQuantity >= 0", constraint.Sql);

        var saved = await AddMedicineAsync(stock: 1, reorderLevel: 1);
        var error = await Assert.ThrowsAsync<SqliteException>(() =>
            _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Medicines SET StockQuantity = {-1} WHERE Id = {saved.Id}"));

        Assert.Equal(19, error.SqliteErrorCode);
        _db.ChangeTracker.Clear();
        Assert.Equal(1, await _db.Medicines.AsNoTracking()
            .Where(m => m.Id == saved.Id)
            .Select(m => m.StockQuantity)
            .SingleAsync());
    }

    [Fact]
    public async Task StockIn_adds_the_quantity_and_saves_the_batch_with_its_expiry_date()
    {
        var medicine = await AddMedicineAsync(stock: 10, reorderLevel: 5);
        var expiry = DateTime.UtcNow.Date.AddMonths(6);

        await _pharmacy.StockInAsync(medicine.Id, "BATCH-001", 25, expiry);

        var reloaded = await _db.Medicines.AsNoTracking().FirstAsync(m => m.Id == medicine.Id);
        Assert.Equal(35, reloaded.StockQuantity);

        var batch = await _db.MedicineBatches.AsNoTracking().SingleAsync(b => b.MedicineId == medicine.Id);
        Assert.Equal("BATCH-001", batch.BatchNumber);
        Assert.Equal(25, batch.Quantity);
        Assert.Equal(expiry, batch.ExpiryDate.Date);
    }

    [Fact]
    public async Task Concurrent_stock_in_requests_preserve_both_increments_and_batches()
    {
        var medicine = await AddMedicineAsync(stock: 10, reorderLevel: 5);
        var connectionString = $"Data Source={_databasePath};Default Timeout=30";
        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlite(connectionString)
            .Options;

        async Task AddStockAsync(string batchNumber, int quantity)
        {
            await using var db = new HmsDbContext(options);
            var service = new PharmacyService(db);
            await service.StockInAsync(
                medicine.Id,
                batchNumber,
                quantity,
                DateTime.UtcNow.Date.AddMonths(6));
        }

        await Task.WhenAll(
            AddStockAsync("CONCURRENT-IN-1", 7),
            AddStockAsync("CONCURRENT-IN-2", 11));

        _db.ChangeTracker.Clear();
        var reloaded = await _db.Medicines.AsNoTracking().SingleAsync(m => m.Id == medicine.Id);
        Assert.Equal(28, reloaded.StockQuantity);

        var batches = await _db.MedicineBatches.AsNoTracking()
            .Where(b => b.MedicineId == medicine.Id &&
                (b.BatchNumber == "CONCURRENT-IN-1" || b.BatchNumber == "CONCURRENT-IN-2"))
            .OrderBy(b => b.BatchNumber)
            .ToListAsync();

        Assert.Collection(
            batches,
            batch => Assert.Equal(7, batch.Quantity),
            batch => Assert.Equal(11, batch.Quantity));
        Assert.Equal(batches.Sum(batch => batch.Quantity), reloaded.StockQuantity - 10);
    }

    [Fact]
    public async Task StockOut_subtracts_the_quantity_from_the_stock()
    {
        var medicine = await AddMedicineAsync(stock: 20, reorderLevel: 5);

        await _pharmacy.StockOutAsync(medicine.Id, 8, "Dispensed");

        var reloaded = await _db.Medicines.AsNoTracking().FirstAsync(m => m.Id == medicine.Id);
        Assert.Equal(12, reloaded.StockQuantity);
    }

    [Fact]
    public async Task StockOut_more_than_the_stock_leaves_the_stock_unchanged_and_says_not_enough_stock()
    {
        var medicine = await AddMedicineAsync(stock: 3, reorderLevel: 1);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _pharmacy.StockOutAsync(medicine.Id, 4, "Dispensed"));

        Assert.Equal("Not enough stock available.", error.Message);

        var reloaded = await _db.Medicines.AsNoTracking().FirstAsync(m => m.Id == medicine.Id);
        Assert.Equal(3, reloaded.StockQuantity);
    }

    [Fact]
    public async Task Concurrent_stock_out_requests_cannot_oversell_or_make_stock_negative()
    {
        var medicine = await AddMedicineAsync(stock: 10, reorderLevel: 2);
        var connectionString = $"Data Source={_databasePath};Default Timeout=30";
        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlite(connectionString)
            .Options;

        async Task<Exception?> TryStockOutAsync()
        {
            await using var db = new HmsDbContext(options);
            var service = new PharmacyService(db);
            try
            {
                await service.StockOutAsync(medicine.Id, 6, "Concurrent dispense");
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var outcomes = await Task.WhenAll(TryStockOutAsync(), TryStockOutAsync());

        Assert.Single(outcomes, outcome => outcome is null);
        var rejected = Assert.Single(outcomes, outcome => outcome is not null);
        var stockError = Assert.IsType<InvalidOperationException>(rejected);
        Assert.Equal("Not enough stock available.", stockError.Message);

        _db.ChangeTracker.Clear();
        var reloaded = await _db.Medicines.AsNoTracking().SingleAsync(m => m.Id == medicine.Id);
        Assert.Equal(4, reloaded.StockQuantity);
        Assert.True(reloaded.StockQuantity >= 0);
    }

    [Fact]
    public async Task Status_is_low_stock_when_the_stock_is_at_or_below_the_reorder_level()
    {
        var atLevel = await AddMedicineAsync(stock: 5, reorderLevel: 5);
        var belowLevel = await AddMedicineAsync(stock: 2, reorderLevel: 5);

        Assert.Equal(EntityStatus.MedicineLowStock, await _pharmacy.GetStatusAsync(atLevel.Id));
        Assert.Equal(EntityStatus.MedicineLowStock, await _pharmacy.GetStatusAsync(belowLevel.Id));
    }

    [Fact]
    public async Task Status_is_out_of_stock_when_the_stock_is_zero()
    {
        var medicine = await AddMedicineAsync(stock: 0, reorderLevel: 5);

        Assert.Equal(EntityStatus.MedicineOutOfStock, await _pharmacy.GetStatusAsync(medicine.Id));
    }

    [Fact]
    public async Task Status_is_in_stock_when_the_stock_is_above_the_reorder_level()
    {
        var medicine = await AddMedicineAsync(stock: 50, reorderLevel: 5);

        Assert.Equal(EntityStatus.MedicineInStock, await _pharmacy.GetStatusAsync(medicine.Id));
    }

    [Fact]
    public async Task Status_is_expired_when_the_earliest_batch_expiry_date_is_before_today()
    {
        var medicine = await AddMedicineAsync(stock: 50, reorderLevel: 5);

        await _pharmacy.StockInAsync(medicine.Id, "OLD-1", 5, DateTime.UtcNow.Date.AddDays(-1));
        await _pharmacy.StockInAsync(medicine.Id, "NEW-1", 5, DateTime.UtcNow.Date.AddYears(1));

        Assert.Equal(EntityStatus.MedicineExpired, await _pharmacy.GetStatusAsync(medicine.Id));
    }

    [Fact]
    public async Task Stock_out_to_zero_sets_the_status_to_out_of_stock()
    {
        var medicine = await AddMedicineAsync(stock: 4, reorderLevel: 2);

        await _pharmacy.StockOutAsync(medicine.Id, 4, "Dispensed");

        var reloaded = await _db.Medicines.AsNoTracking().FirstAsync(m => m.Id == medicine.Id);
        Assert.Equal(0, reloaded.StockQuantity);
        Assert.Equal(EntityStatus.MedicineOutOfStock, reloaded.Status);
    }

    [Fact]
    public async Task Inventory_report_lists_every_medicine_with_current_status_expiry_and_counts()
    {
        var category = new MedicineCategory { Name = $"Category {Guid.NewGuid():N}" };
        _db.MedicineCategories.Add(category);
        await _db.SaveChangesAsync();

        var lowStock = await AddMedicineAsync(stock: 2, reorderLevel: 5);
        lowStock.CategoryId = category.Id;
        var outOfStock = await AddMedicineAsync(stock: 0, reorderLevel: 5);
        outOfStock.CategoryId = category.Id;
        var expired = await AddMedicineAsync(stock: 20, reorderLevel: 5);
        expired.CategoryId = category.Id;
        await _db.SaveChangesAsync();

        var expiry = DateTime.UtcNow.Date.AddDays(-1);
        _db.MedicineBatches.Add(new MedicineBatch
        {
            MedicineId = expired.Id,
            BatchNumber = $"EXPIRED-{Guid.NewGuid():N}",
            Quantity = 1,
            ExpiryDate = expiry
        });
        await _db.SaveChangesAsync();

        var controller = new MedicineController(_db, _pharmacy);

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var report = Assert.IsType<MedicineIndexViewModel>(result.Model);

        Assert.Contains(report.Medicines, item =>
            item.Id == lowStock.Id &&
            item.Category == category.Name &&
            item.StockQuantity == 2 &&
            item.Status == EntityStatus.MedicineLowStock);
        Assert.Contains(report.Medicines, item =>
            item.Id == outOfStock.Id &&
            item.StockQuantity == 0 &&
            item.Status == EntityStatus.MedicineOutOfStock);
        Assert.Contains(report.Medicines, item =>
            item.Id == expired.Id &&
            item.Status == EntityStatus.MedicineExpired &&
            item.EarliestExpiryDate == expiry);
        Assert.Equal(1, report.LowStockCount);
        Assert.Equal(1, report.OutOfStockCount);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
