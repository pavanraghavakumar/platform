using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Doctor)]
public class ConsultationController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IConsultationService _consultations;

    public ConsultationController(HmsDbContext db, IConsultationService consultations)
    {
        _db = db;
        _consultations = consultations;
    }

    public async Task<IActionResult> Index(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
        {
            return NotFound();
        }

        ViewData["Record"] = await _consultations.GetByAppointmentAsync(appointmentId);
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MedicalRecord record)
    {
        try
        {
            await _consultations.SaveDraftAsync(record);
            return RedirectToAction(nameof(Index), new { appointmentId = record.AppointmentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction(nameof(Index), new { appointmentId = record.AppointmentId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        await _consultations.FinalizeAsync(id);
        return RedirectToAction("Index", "Dashboard");
    }
}
