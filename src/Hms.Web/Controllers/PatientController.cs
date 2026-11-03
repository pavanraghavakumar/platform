using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Dashboard;
using Hms.Web.ViewModels.Patient;
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
    public async Task<IActionResult> Index(string? search, string? status, string? sort, int page = 1)
    {
        var model = await _patients.GetPageAsync(search, status, sort, page, PatientListViewModel.PageSize);
        return View(model);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpGet]
    public IActionResult Create() => View(new PatientCreateViewModel());

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError(string.Empty, "Full name and phone number are required.");
            return View(model);
        }

        var hasEmail = !string.IsNullOrWhiteSpace(model.Email);
        var hasPassword = !string.IsNullOrWhiteSpace(model.Password);
        if (hasEmail != hasPassword)
        {
            ModelState.AddModelError(string.Empty, "Email and sign-in password must be provided together.");
            return View(model);
        }

        var patient = new Patient
        {
            FullName = model.FullName,
            DateOfBirth = model.DateOfBirth ?? default,
            Gender = model.Gender?.Trim() ?? string.Empty,
            Phone = model.Phone,
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim(),
            EmergencyContactName = model.EmergencyContactName?.Trim(),
            EmergencyContactPhone = model.EmergencyContactPhone?.Trim(),
            MedicalHistory = model.MedicalHistory,
            Allergies = model.Allergies
        };

        try
        {
            var created = hasEmail
                ? await _patients.RegisterWithAccountAsync(patient, model.Password!)
                : await _patients.RegisterAsync(patient);
            TempData["Message"] = $"Patient {created.PatientCode} registered.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Doctor)]
    public async Task<IActionResult> Details(int id)
    {
        if (User.IsInRole(Roles.Doctor))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var hasAppointment = await _db.Appointments
                .AsNoTracking()
                .AnyAsync(a => a.PatientId == id && a.Doctor!.UserId == userId);

            if (!hasAppointment)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var model = await _patients.GetProfileAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
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

        return View(ToEditModel(patient));
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var updated = await _patients.UpdateContactAsync(model);
        if (updated is null)
        {
            return NotFound();
        }

        ViewData["Message"] = "Patient details saved.";
        return View(ToEditModel(updated));
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var deactivated = await _patients.DeactivateAsync(id);
        if (!deactivated)
        {
            return NotFound();
        }

        TempData["Message"] = "Patient deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> DoctorPatients()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var doctorId = await _db.Doctors
            .Where(d => d.UserId == userId)
            .Select(d => (int?)d.Id)
            .SingleOrDefaultAsync();

        if (doctorId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var patients = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId.Value)
            .GroupBy(a => new { a.PatientId, a.Patient!.PatientCode, a.Patient.FullName, a.Patient.Status })
            .Select(group => new DoctorPatientRow
            {
                Id = group.Key.PatientId,
                PatientCode = group.Key.PatientCode,
                FullName = group.Key.FullName,
                LastAppointmentDate = group.Max(a => a.AppointmentDate),
                Status = group.Key.Status
            })
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return View(patients);
    }

    private static PatientEditViewModel ToEditModel(Patient patient) => new()
    {
        Id = patient.Id,
        PatientCode = patient.PatientCode,
        FullName = patient.FullName,
        Phone = patient.Phone,
        Email = patient.Email,
        Address = patient.Address,
        EmergencyContactName = patient.EmergencyContactName,
        EmergencyContactPhone = patient.EmergencyContactPhone,
        MedicalHistory = patient.MedicalHistory,
        Allergies = patient.Allergies
    };
}
