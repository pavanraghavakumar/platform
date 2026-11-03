using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
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

    public async Task<IActionResult> Index()
    {
        return View(await _db.Medicines.Include(m => m.Category).Include(m => m.Supplier).OrderBy(m => m.Name).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Categories"] = await _db.MedicineCategories.OrderBy(c => c.Name).ToListAsync();
        ViewData["Suppliers"] = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
        return View(new Medicine());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine medicine)
    {
        if (string.IsNullOrWhiteSpace(medicine.Name))
        {
            ModelState.AddModelError(nameof(Medicine.Name), "Name is required.");
            ViewData["Categories"] = await _db.MedicineCategories.OrderBy(c => c.Name).ToListAsync();
            ViewData["Suppliers"] = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return View(medicine);
        }

        medicine.Status = EntityStatus.MedicineInStock;
        medicine.CreatedAt = DateTime.UtcNow;
        medicine.UpdatedAt = DateTime.UtcNow;
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();
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

        return View(medicine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(int id, string batchNumber, int quantity, DateTime expiryDate)
    {
        await _pharmacy.StockInAsync(id, batchNumber, quantity, expiryDate);
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

        return View(medicine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockOut(int id, int quantity, string? reason)
    {
        try
        {
            await _pharmacy.StockOutAsync(id, quantity, reason);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var medicine = await _db.Medicines.FindAsync(id);
            return View(medicine);
        }
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
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.MedicineCategories.Add(new MedicineCategory { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
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
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.Suppliers.Add(new Supplier { Name = name, Phone = phone, Email = email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Supplier));
    }
}
