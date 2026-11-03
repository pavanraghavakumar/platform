using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class StaffController : Controller
{
    private readonly HmsDbContext _db;

    public StaffController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var staff = await _db.Staff.Include(s => s.Department).OrderBy(s => s.FullName).ToListAsync();
        return View(staff);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(new Staff());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Staff staff)
    {
        if (string.IsNullOrWhiteSpace(staff.FullName) || string.IsNullOrWhiteSpace(staff.Email))
        {
            ModelState.AddModelError(string.Empty, "Full name and email are required.");
            ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
            return View(staff);
        }

        staff.Status = EntityStatus.Active;
        staff.CreatedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var staff = await _db.Staff.FindAsync(id);
        if (staff is not null)
        {
            staff.Status = EntityStatus.Inactive;
            staff.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
