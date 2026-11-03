using Hms.Web.Data;
using Hms.Web.Models.Entities;
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
    public IActionResult Create() => View(new LaboratoryTest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LaboratoryTest test)
    {
        if (string.IsNullOrWhiteSpace(test.Name))
        {
            ModelState.AddModelError(nameof(LaboratoryTest.Name), "Name is required.");
            return View(test);
        }

        test.CreatedAt = DateTime.UtcNow;
        test.UpdatedAt = DateTime.UtcNow;
        _db.LaboratoryTests.Add(test);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
