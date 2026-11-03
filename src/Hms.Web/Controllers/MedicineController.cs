using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Pharmacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class MedicineController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IPharmacyService _pharmacy;

    public MedicineController(HmsDbContext db, IPharmacyService pharmacy)
    {
        _db = db;
        _pharmacy = pharmacy;
    }

    /// <summary>The medicine list with status badges and the inventory report counts (Requirements 9.6–9.9).</summary>
    public async Task<IActionResult> Index()
    {
        await _pharmacy.RefreshAllStatusesAsync();

        var medicines = await _db.Medicines
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .Include(m => m.Batches)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var items = medicines.Select(m => new MedicineListItemViewModel
        {
            Id = m.Id,
            Name = m.Name,
            Category = m.Category?.Name ?? string.Empty,
            Supplier = m.Supplier?.Name ?? string.Empty,
            UnitPrice = m.UnitPrice,
            ReorderLevel = m.ReorderLevel,
            StockQuantity = m.StockQuantity,
            Status = m.Status,
            EarliestExpiryDate = m.Batches.Count == 0 ? null : m.Batches.Min(b => b.ExpiryDate)
        }).ToList();

        var model = new MedicineIndexViewModel
        {
            Medicines = items,
            LowStockCount = items.Count(i => i.Status == EntityStatus.MedicineLowStock),
            OutOfStockCount = items.Count(i => i.Status == EntityStatus.MedicineOutOfStock),
            ExpiredCount = items.Count(i => i.Status == EntityStatus.MedicineExpired)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new MedicineCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicineCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(MedicineCreateViewModel.Name), "Name is required.");
        }

        if (model.StockQuantity < 0)
        {
            ModelState.AddModelError(
                nameof(MedicineCreateViewModel.StockQuantity),
                "Stock quantity cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        var medicine = new Medicine
        {
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId,
            SupplierId = model.SupplierId,
            UnitPrice = model.UnitPrice,
            ReorderLevel = model.ReorderLevel,
            StockQuantity = model.StockQuantity,
            Status = EntityStatus.MedicineInStock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Medicine saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> StockIn(int id)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        return View(new StockInViewModel
        {
            MedicineId = medicine.Id,
            MedicineName = medicine.Name,
            ExpiryDate = DateTime.UtcNow.Date.AddYears(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(int id, StockInViewModel model)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        model.MedicineId = medicine.Id;
        model.MedicineName = medicine.Name;

        if (string.IsNullOrWhiteSpace(model.BatchNumber))
        {
            ModelState.AddModelError(nameof(StockInViewModel.BatchNumber), "Batch number is required.");
        }

        if (model.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(StockInViewModel.Quantity), "Quantity must be greater than zero.");
        }

        if (model.ExpiryDate is null)
        {
            ModelState.AddModelError(nameof(StockInViewModel.ExpiryDate), "Expiry date is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _pharmacy.StockInAsync(medicine.Id, model.BatchNumber.Trim(), model.Quantity, model.ExpiryDate!.Value);

        TempData["Message"] = "Stock added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> StockOut(int id)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        return View(new StockOutViewModel
        {
            MedicineId = medicine.Id,
            MedicineName = medicine.Name,
            StockQuantity = medicine.StockQuantity
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockOut(int id, StockOutViewModel model)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        model.MedicineId = medicine.Id;
        model.MedicineName = medicine.Name;
        model.StockQuantity = medicine.StockQuantity;

        if (model.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(StockOutViewModel.Quantity), "Quantity must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _pharmacy.StockOutAsync(medicine.Id, model.Quantity, model.Reason);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["Message"] = "Stock removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Category()
    {
        return View(await _db.MedicineCategories.OrderBy(c => c.Name).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Category(string name)
    {
        var trimmed = name?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var exists = await _db.MedicineCategories.AnyAsync(c => c.Name == trimmed);
            if (!exists)
            {
                _db.MedicineCategories.Add(new MedicineCategory
                {
                    Name = trimmed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                TempData["Message"] = "Category saved.";
            }
            else
            {
                TempData["Error"] = "A category with this name already exists.";
            }
        }

        return RedirectToAction(nameof(Category));
    }

    [HttpGet]
    public async Task<IActionResult> Supplier()
    {
        return View(await _db.Suppliers.OrderBy(s => s.Name).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Supplier(string name, string? phone, string? email)
    {
        var trimmed = name?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var exists = await _db.Suppliers.AnyAsync(s => s.Name == trimmed);
            if (!exists)
            {
                _db.Suppliers.Add(new Supplier
                {
                    Name = trimmed,
                    Phone = phone,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                TempData["Message"] = "Supplier saved.";
            }
            else
            {
                TempData["Error"] = "A supplier with this name already exists.";
            }
        }

        return RedirectToAction(nameof(Supplier));
    }

    private async Task LoadLookupsAsync()
    {
        ViewData["Categories"] = await _db.MedicineCategories.OrderBy(c => c.Name).ToListAsync();
        ViewData["Suppliers"] = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
    }
}
