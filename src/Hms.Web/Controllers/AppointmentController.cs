using System.Security.Claims;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Appointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AppointmentEntity = Hms.Web.Models.Entities.Appointment;

namespace Hms.Web.Controllers;

[Authorize]
public class AppointmentController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IAppointmentService _appointments;

    public AppointmentController(HmsDbContext db, IAppointmentService appointments)
    {
        _db = db;
        _appointments = appointments;
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Doctor + "," + Roles.Patient)]
    public async Task<IActionResult> Index(string? status)
    {
        // Admin may inspect the list, but design.md reserves every status-changing
        // appointment action for Receptionist, Patient (own cancel), or Doctor.
        var isReceptionist = User.IsInRole(Roles.Receptionist);
        var isDoctor = User.IsInRole(Roles.Doctor);
        var isPatient = User.IsInRole(Roles.Patient);

        var query = _db.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();

        if (isDoctor && !isReceptionist)
        {
            var doctorId = await CurrentDoctorIdAsync();
            query = query.Where(a => a.DoctorId == doctorId);
        }
        else if (isPatient && !isReceptionist)
        {
            var patientId = await CurrentPatientIdAsync();
            query = query.Where(a => a.PatientId == patientId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        // SQLite cannot ORDER BY a TimeSpan column, so order by date in SQL and by time in memory.
        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
        appointments = appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return View(new AppointmentListViewModel
        {
            Status = status,
            IsReceptionist = isReceptionist,
            IsDoctor = isDoctor,
            IsPatient = isPatient,
            Appointments = appointments
        });
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpGet]
    public async Task<IActionResult> Book()
    {
        return View(await BuildBookViewModelAsync(new BookAppointmentViewModel()));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        var isReceptionist = User.IsInRole(Roles.Receptionist);

        if (!TimeSpan.TryParse(model.Time, out var startTime))
        {
            ModelState.AddModelError(string.Empty, "Choose a time slot.");
            return View(await BuildBookViewModelAsync(model));
        }

        if (model.DoctorId is null || model.Date is null)
        {
            ModelState.AddModelError(string.Empty, "Choose a doctor and a date.");
            return View(await BuildBookViewModelAsync(model));
        }

        var patientId = model.PatientId ?? 0;
        if (!isReceptionist)
        {
            var ownPatientId = await CurrentPatientIdAsync();
            if (model.PatientId is not null && model.PatientId != ownPatientId)
            {
                ModelState.AddModelError(string.Empty, "You can only book appointments for yourself.");
                return View(await BuildBookViewModelAsync(model));
            }

            patientId = ownPatientId ?? 0;
        }

        try
        {
            await _appointments.BookForUserAsync(
                CurrentUserId(),
                isReceptionist,
                patientId,
                model.DoctorId.Value,
                model.Date.Value,
                startTime,
                model.Notes);

            TempData["Message"] = "Appointment booked.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildBookViewModelAsync(model));
        }
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpGet]
    public async Task<IActionResult> Slots(int doctorId, DateTime date)
    {
        var slots = await _appointments.GetFreeSlotsAsync(doctorId, date);
        return Json(new { slots });
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        if (await _appointments.ConfirmAsync(id))
        {
            TempData["Message"] = "Appointment confirmed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        if (await _appointments.CheckInAsync(id))
        {
            TempData["Message"] = "Appointment checked in.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(int id)
    {
        if (await _appointments.MarkNoShowAsync(id))
        {
            TempData["Message"] = "Appointment marked as No Show.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var isReceptionist = User.IsInRole(Roles.Receptionist);
        if (await _appointments.CancelAsync(id, CurrentUserId(), isReceptionist))
        {
            TempData["Message"] = "Appointment cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        var ownerDoctorId = await _db.Appointments
            .Where(a => a.Id == id)
            .Select(a => (int?)a.DoctorId)
            .FirstOrDefaultAsync();
        if (ownerDoctorId is null)
        {
            return NotFound();
        }

        if (doctorId is null || ownerDoctorId != doctorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!await _appointments.StartAsync(id, doctorId.Value))
        {
            return RedirectToAction("Doctor", "Dashboard");
        }

        TempData["Message"] = "Consultation started.";
        return RedirectToAction("Index", "Consultation", new { appointmentId = id });
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        var ownerDoctorId = await _db.Appointments
            .Where(a => a.Id == id)
            .Select(a => (int?)a.DoctorId)
            .FirstOrDefaultAsync();
        if (ownerDoctorId is null)
        {
            return NotFound();
        }

        if (doctorId is null || ownerDoctorId != doctorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (await _appointments.CompleteAsync(id, doctorId.Value))
        {
            TempData["Message"] = "Consultation completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<BookAppointmentViewModel> BuildBookViewModelAsync(BookAppointmentViewModel model)
    {
        var isReceptionist = User.IsInRole(Roles.Receptionist);
        model.CanChoosePatient = isReceptionist;

        model.Doctors = await _db.Doctors
            .Where(d => d.Status == EntityStatus.Active)
            .OrderBy(d => d.FullName)
            .ToListAsync();

        if (isReceptionist)
        {
            model.Patients = await _db.Patients
                .Where(p => p.Status == EntityStatus.Active)
                .OrderBy(p => p.FullName)
                .ToListAsync();
        }
        else
        {
            var patientId = await CurrentPatientIdAsync();
            model.OwnPatient = patientId is null
                ? null
                : await _db.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
            if (model.OwnPatient is not null)
            {
                model.PatientId = model.OwnPatient.Id;
            }
        }

        return model;
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private Task<int?> CurrentPatientIdAsync()
    {
        var userId = CurrentUserId();
        return _db.Patients.Where(p => p.UserId == userId).Select(p => (int?)p.Id).FirstOrDefaultAsync();
    }

    private Task<int?> CurrentDoctorIdAsync()
    {
        var userId = CurrentUserId();
        return _db.Doctors.Where(d => d.UserId == userId).Select(d => (int?)d.Id).FirstOrDefaultAsync();
    }
}
