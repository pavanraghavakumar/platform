using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize]
public class PatientController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IPatientService _patients;

    public PatientController(HmsDbContext db, IPatientService patients)
    {
        _db = db;
        _patients = patients;
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        const int pageSize = 10;
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.FullName.Contains(term) || p.PatientCode.Contains(term) || p.Phone.Contains(term));
        }

        ViewData["Search"] = search;
        ViewData["Page"] = page;
        return View(await query.OrderBy(p => p.Id).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpGet]
    public IActionResult Create() => View(new Patient());

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FullName) || string.IsNullOrWhiteSpace(patient.Phone))
        {
            ModelState.AddModelError(string.Empty, "Full name and phone number are required.");
            return View(patient);
        }

        await _patients.RegisterAsync(patient);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Doctor)]
    public async Task<IActionResult> Details(int id)
    {
        var patient = await _db.Patients
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return NotFound();
        }

        return View(patient);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null)
        {
            return NotFound();
        }

        return View(patient);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Patient patient)
    {
        var existing = await _db.Patients.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Phone = patient.Phone;
        existing.Email = patient.Email;
        existing.Address = patient.Address;
        existing.EmergencyContactName = patient.EmergencyContactName;
        existing.EmergencyContactPhone = patient.EmergencyContactPhone;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        ViewData["Message"] = "Patient details saved.";
        return View(existing);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is not null)
        {
            patient.Status = EntityStatus.Inactive;
            patient.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> DoctorPatients()
    {
        var patients = await _db.Patients.OrderBy(p => p.FullName).ToListAsync();
        return View(patients);
    }
}
