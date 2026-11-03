using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Lab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize]
public class LabRequestController : Controller
{
    private readonly HmsDbContext _db;
    private readonly ILabService _lab;
    private readonly UserManager<ApplicationUser> _users;

    public LabRequestController(HmsDbContext db, ILabService lab, UserManager<ApplicationUser> users)
    {
        _db = db;
        _lab = lab;
        _users = users;
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    public async Task<IActionResult> Index(string? status)
    {
        var requests = await _lab.GetRequestsAsync(status);
        return View(new LabRequestListViewModel { Requests = requests, Status = status });
    }

    /// <summary>The request form a Doctor uses during a consultation (Requirement 8.2).</summary>
    [Authorize(Roles = Roles.Doctor)]
    [HttpGet]
    public async Task<IActionResult> Create(int patientId, int? appointmentId)
    {
        var doctor = await CurrentDoctorAsync();
        if (doctor is null)
        {
            return Forbid();
        }

        if (!appointmentId.HasValue)
        {
            return NotFound();
        }

        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.DoctorId != doctor.Id || appointment.PatientId != patientId)
        {
            return Forbid();
        }

        return View(await BuildCreateModelAsync(appointment.Patient!, doctor, appointment.Id));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int patientId, int? appointmentId, int[]? testIds)
    {
        var doctor = await CurrentDoctorAsync();
        if (doctor is null)
        {
            return Forbid();
        }

        if (!appointmentId.HasValue)
        {
            return NotFound();
        }

        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.DoctorId != doctor.Id || appointment.PatientId != patientId)
        {
            return Forbid();
        }

        if (testIds is null || testIds.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one test.");
            var model = await BuildCreateModelAsync(appointment.Patient!, doctor, appointment.Id);
            return View(model);
        }

        var created = await _lab.CreateRequestsAsync(appointment.PatientId, appointment.DoctorId, appointment.Id, testIds);
        TempData["Message"] = "Laboratory tests requested.";
        return RedirectToAction(nameof(Details), new { id = created[0].Id });
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var started = await _lab.StartAsync(id);
        if (!started)
        {
            TempData["Error"] = "Only a Requested lab request can be started.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var request = await _lab.GetRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != EntityStatus.LabInProgress)
        {
            TempData["Error"] = "Only an In Progress lab request can be completed.";
            return RedirectToAction(nameof(Index));
        }

        return View(new LabResultViewModel { Request = request });
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? resultValue, string? remarks)
    {
        var request = await _lab.GetRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(resultValue))
        {
            ModelState.AddModelError(nameof(LabResultViewModel.ResultValue), "Result value is required.");
            return View(new LabResultViewModel { Request = request, ResultValue = resultValue, Remarks = remarks });
        }

        var completed = await _lab.CompleteAsync(id, resultValue.Trim(), string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim());
        if (!completed)
        {
            TempData["Error"] = "Only an In Progress lab request can be completed.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Message"] = "Laboratory result saved.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Patient + "," + Roles.Admin)]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _lab.GetRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (!await CanViewAsync(request))
        {
            return Forbid();
        }

        return View(request);
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Patient)]
    public async Task<IActionResult> Print(int id)
    {
        var request = await _lab.GetRequestAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (!await CanViewAsync(request))
        {
            return Forbid();
        }

        return View(request);
    }

    /// <summary>
    /// A Doctor may open only the requests that Doctor made (Requirement 8.7); a Patient only
    /// their own (Requirement 8.8); an Admin may open any.
    /// </summary>
    private async Task<bool> CanViewAsync(LabRequest request)
    {
        if (User.IsInRole(Roles.Admin))
        {
            return true;
        }

        var userId = _users.GetUserId(User);
        if (User.IsInRole(Roles.Doctor))
        {
            return await _lab.IsRequestedByDoctorAsync(request.Id, userId);
        }

        if (User.IsInRole(Roles.Patient))
        {
            return request.Status == EntityStatus.LabCompleted
                && await _lab.IsOwnedByPatientAsync(request.Id, userId);
        }

        return false;
    }

    private async Task<Doctor?> CurrentDoctorAsync()
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    private async Task<LabRequestCreateViewModel> BuildCreateModelAsync(Patient patient, Doctor doctor, int? appointmentId)
    {
        return new LabRequestCreateViewModel
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentId = appointmentId,
            PatientName = patient.FullName,
            AvailableTests = await _db.LaboratoryTests.OrderBy(t => t.Name).ToListAsync()
        };
    }
}
