using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DepartmentController : Controller
{
    private readonly HmsDbContext _db;

    public DepartmentController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(departments);
    }

    [HttpGet]
    public IActionResult Create() => View(new Department());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            ModelState.AddModelError(nameof(Department.Name), "Name is required.");
            return View(department);
        }

        if (await _db.Departments.AnyAsync(d => d.Name == department.Name))
        {
            ModelState.AddModelError(nameof(Department.Name), "A department with this name already exists.");
            return View(department);
        }

        department.Status = EntityStatus.Active;
        department.CreatedAt = DateTime.UtcNow;
        department.UpdatedAt = DateTime.UtcNow;
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department is not null)
        {
            department.Status = EntityStatus.Inactive;
            department.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
