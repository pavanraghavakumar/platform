using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DoctorController : Controller
{
    private readonly HmsDbContext _db;

    public DoctorController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? departmentId)
    {
        var query = _db.Doctors.Include(d => d.Department).AsQueryable();
        if (departmentId is not null)
        {
            query = query.Where(d => d.DepartmentId == departmentId);
        }

        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        ViewData["DepartmentId"] = departmentId;
        return View(await query.OrderBy(d => d.FullName).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(new Doctor());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Doctor doctor)
    {
        if (string.IsNullOrWhiteSpace(doctor.FullName) || string.IsNullOrWhiteSpace(doctor.Email))
        {
            ModelState.AddModelError(string.Empty, "Full name and email are required.");
            ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
            return View(doctor);
        }

        doctor.Status = EntityStatus.Active;
        doctor.CreatedAt = DateTime.UtcNow;
        doctor.UpdatedAt = DateTime.UtcNow;
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.Schedules).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return NotFound();
        }

        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Doctor doctor)
    {
        var existing = await _db.Doctors.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.FullName = doctor.FullName;
        existing.Email = doctor.Email;
        existing.Phone = doctor.Phone;
        existing.Specialization = doctor.Specialization;
        existing.DepartmentId = doctor.DepartmentId;
        existing.Qualifications = doctor.Qualifications;
        existing.YearsOfExperience = doctor.YearsOfExperience;
        existing.ConsultationFee = doctor.ConsultationFee;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.Department)
            .Include(d => d.Schedules)
            .Include(d => d.Appointments)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return NotFound();
        }

        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is not null)
        {
            doctor.Status = EntityStatus.Inactive;
            doctor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
