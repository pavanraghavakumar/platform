using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Lab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class LabTestController : Controller
{
    private readonly HmsDbContext _db;

    public LabTestController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _db.LaboratoryTests.OrderBy(t => t.Name).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new LabTestFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LabTestFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            ModelState.AddModelError(nameof(LabTestFormViewModel.Name), "Name is required.");
        }

        if (form.Price < 0)
        {
            ModelState.AddModelError(nameof(LabTestFormViewModel.Price), "Price cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var now = DateTime.UtcNow;
        _db.LaboratoryTests.Add(new LaboratoryTest
        {
            Name = form.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(form.Category) ? null : form.Category.Trim(),
            Price = form.Price,
            NormalRange = string.IsNullOrWhiteSpace(form.NormalRange) ? null : form.NormalRange.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "Laboratory test saved.";
        return RedirectToAction(nameof(Index));
    }
}
