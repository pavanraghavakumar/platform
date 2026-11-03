using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPharmacyService
{
    Task StockInAsync(int medicineId, string batchNumber, int quantity, DateTime expiryDate);

    Task StockOutAsync(int medicineId, int quantity, string? reason);

    Task<string> GetStatusAsync(int medicineId);

    /// <summary>Computes the status of a medicine from its stock and its earliest batch expiry date.</summary>
    Task<string> ComputeStatusAsync(Medicine medicine);

    /// <summary>Recomputes and saves the status of every medicine, so the list and the report stay current.</summary>
    Task RefreshAllStatusesAsync();
}

public class PharmacyService : IPharmacyService
{
    private readonly HmsDbContext _db;

    public PharmacyService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task StockInAsync(int medicineId, string batchNumber, int quantity, DateTime expiryDate)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var updatedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Increment in the database so concurrent stock-in requests cannot overwrite each
        // other's quantities. The increment and batch insert share this transaction, keeping
        // the aggregate stock consistent with the batches that were actually recorded.
        var affectedRows = await _db.Medicines
            .Where(m => m.Id == medicineId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.StockQuantity, m => m.StockQuantity + quantity)
                .SetProperty(m => m.UpdatedAt, updatedAt));

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Medicine not found.");
        }

        _db.MedicineBatches.Add(new MedicineBatch
        {
            MedicineId = medicineId,
            BatchNumber = batchNumber,
            Quantity = quantity,
            ExpiryDate = expiryDate.Date,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        });

        await _db.SaveChangesAsync();

        var medicine = await _db.Medicines.AsNoTracking().SingleAsync(m => m.Id == medicineId);
        medicine.Status = await ComputeStatusAsync(medicine);

        await _db.Medicines
            .Where(m => m.Id == medicineId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.Status, medicine.Status));

        await transaction.CommitAsync();

        // ExecuteUpdate bypasses tracking. Synchronize a controller- or test-loaded instance
        // so a later SaveChanges cannot restore the stock value from before this operation.
        var trackedMedicine = _db.Medicines.Local.FirstOrDefault(m => m.Id == medicineId);
        if (trackedMedicine is not null)
        {
            trackedMedicine.StockQuantity = medicine.StockQuantity;
            trackedMedicine.Status = medicine.Status;
            trackedMedicine.UpdatedAt = updatedAt;
            _db.Entry(trackedMedicine).Property(m => m.StockQuantity).IsModified = false;
            _db.Entry(trackedMedicine).Property(m => m.Status).IsModified = false;
            _db.Entry(trackedMedicine).Property(m => m.UpdatedAt).IsModified = false;
        }
    }

    public async Task StockOutAsync(int medicineId, int quantity, string? reason)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var updatedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // The stock check and decrement are one SQL statement. SQLite serializes writers, so a
        // concurrent request evaluates this predicate against the quantity committed by the
        // preceding writer instead of an earlier value loaded into application memory.
        var affectedRows = await _db.Medicines
            .Where(m => m.Id == medicineId && m.StockQuantity >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.StockQuantity, m => m.StockQuantity - quantity)
                .SetProperty(m => m.UpdatedAt, updatedAt));

        if (affectedRows == 0)
        {
            var exists = await _db.Medicines.AsNoTracking().AnyAsync(m => m.Id == medicineId);
            await transaction.RollbackAsync();
            throw new InvalidOperationException(exists
                ? "Not enough stock available."
                : "Medicine not found.");
        }

        var medicine = await _db.Medicines.AsNoTracking().SingleAsync(m => m.Id == medicineId);
        medicine.Status = await ComputeStatusAsync(medicine);

        await _db.Medicines
            .Where(m => m.Id == medicineId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.Status, medicine.Status));

        await transaction.CommitAsync();

        // ExecuteUpdate bypasses the change tracker. Keep a controller-loaded entity synchronized
        // and unchanged so a later SaveChanges cannot write its stale stock value back to the row.
        var trackedMedicine = _db.Medicines.Local.FirstOrDefault(m => m.Id == medicineId);
        if (trackedMedicine is not null)
        {
            trackedMedicine.StockQuantity = medicine.StockQuantity;
            trackedMedicine.Status = medicine.Status;
            trackedMedicine.UpdatedAt = updatedAt;
            _db.Entry(trackedMedicine).Property(m => m.StockQuantity).IsModified = false;
            _db.Entry(trackedMedicine).Property(m => m.Status).IsModified = false;
            _db.Entry(trackedMedicine).Property(m => m.UpdatedAt).IsModified = false;
        }
    }

    public async Task<string> GetStatusAsync(int medicineId)
    {
        var medicine = await _db.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        return await ComputeStatusAsync(medicine);
    }

    public async Task<string> ComputeStatusAsync(Medicine medicine)
    {
        var earliestExpiry = await _db.MedicineBatches
            .Where(b => b.MedicineId == medicine.Id)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => (DateTime?)b.ExpiryDate)
            .FirstOrDefaultAsync();

        if (earliestExpiry is not null && earliestExpiry.Value.Date < DateTime.UtcNow.Date)
        {
            return EntityStatus.MedicineExpired;
        }

        if (medicine.StockQuantity == 0)
        {
            return EntityStatus.MedicineOutOfStock;
        }

        if (medicine.StockQuantity <= medicine.ReorderLevel)
        {
            return EntityStatus.MedicineLowStock;
        }

        return EntityStatus.MedicineInStock;
    }

    public async Task RefreshAllStatusesAsync()
    {
        var medicines = await _db.Medicines.ToListAsync();
        foreach (var medicine in medicines)
        {
            medicine.Status = await ComputeStatusAsync(medicine);
        }

        await _db.SaveChangesAsync();
    }

}
