using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    public async Task<IActionResult> Index(string? status)
    {
        var query = _db.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        ViewData["Status"] = status;
        return View(await query.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToListAsync());
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpGet]
    public async Task<IActionResult> Book()
    {
        ViewData["Doctors"] = await _db.Doctors.Where(d => d.Status == EntityStatus.Active).OrderBy(d => d.FullName).ToListAsync();
        ViewData["Patients"] = await _db.Patients.Where(p => p.Status == EntityStatus.Active).OrderBy(p => p.FullName).ToListAsync();
        return View();
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int patientId, int doctorId, DateTime date, string time)
    {
        if (!TimeSpan.TryParse(time, out var startTime))
        {
            ModelState.AddModelError(string.Empty, "Choose a time slot.");
            ViewData["Doctors"] = await _db.Doctors.Where(d => d.Status == EntityStatus.Active).OrderBy(d => d.FullName).ToListAsync();
            ViewData["Patients"] = await _db.Patients.Where(p => p.Status == EntityStatus.Active).OrderBy(p => p.FullName).ToListAsync();
            return View();
        }

        try
        {
            await _appointments.BookAsync(patientId, doctorId, date, startTime, null);
            TempData["Message"] = "Appointment booked.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Doctors"] = await _db.Doctors.Where(d => d.Status == EntityStatus.Active).OrderBy(d => d.FullName).ToListAsync();
            ViewData["Patients"] = await _db.Patients.Where(p => p.Status == EntityStatus.Active).OrderBy(p => p.FullName).ToListAsync();
            return View();
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
    public Task<IActionResult> Confirm(int id) => Transition(id, EntityStatus.AppointmentPending, EntityStatus.AppointmentConfirmed);

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CheckIn(int id) => Transition(id, EntityStatus.AppointmentConfirmed, EntityStatus.AppointmentCheckedIn);

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> NoShow(int id) => Transition(id, EntityStatus.AppointmentConfirmed, EntityStatus.AppointmentNoShow);

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Patient)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is not null
            && (appointment.Status == EntityStatus.AppointmentPending || appointment.Status == EntityStatus.AppointmentConfirmed))
        {
            appointment.Status = EntityStatus.AppointmentCancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Start(int id) => Transition(id, EntityStatus.AppointmentCheckedIn, EntityStatus.AppointmentInProgress);

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(int id) => Transition(id, EntityStatus.AppointmentInProgress, EntityStatus.AppointmentCompleted);

    private async Task<IActionResult> Transition(int id, string fromStatus, string toStatus)
    {
        await _appointments.TryTransitionAsync(id, fromStatus, toStatus);
        return RedirectToAction(nameof(Index));
    }
}
