using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Doctor)]
public class ConsultationController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IConsultationService _consultations;
    private readonly UserManager<ApplicationUser> _users;

    public ConsultationController(HmsDbContext db, IConsultationService consultations, UserManager<ApplicationUser> users)
    {
        _db = db;
        _consultations = consultations;
        _users = users;
    }

    public async Task<IActionResult> Index(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null || appointment.Patient is null)
        {
            return NotFound();
        }

        if (!await IsOwnedByCurrentDoctorAsync(appointment))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!IsConsultationStatus(appointment.Status))
        {
            return NotFound();
        }

        var record = await _consultations.GetByAppointmentAsync(appointmentId);
        var history = await _consultations.GetRecentConsultationsAsync(appointment.PatientId, 5);

        var model = new ConsultationPageViewModel
        {
            AppointmentId = appointment.Id,
            AppointmentStatus = appointment.Status,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            PatientId = appointment.PatientId,
            PatientCode = appointment.Patient.PatientCode,
            PatientName = appointment.Patient.FullName,
            PatientGender = appointment.Patient.Gender,
            PatientDateOfBirth = appointment.Patient.DateOfBirth,
            PatientPhone = appointment.Patient.Phone,
            PatientEmail = appointment.Patient.Email,
            PatientAddress = appointment.Patient.Address,
            Allergies = appointment.Patient.Allergies,
            MedicalHistory = appointment.Patient.MedicalHistory,
            DoctorName = appointment.Doctor?.FullName ?? string.Empty,
            RecentConsultations = history
                .Select(r => new ConsultationHistoryItem
                {
                    MedicalRecordId = r.Id,
                    Date = r.CreatedAt,
                    Diagnosis = r.Diagnosis,
                    DoctorName = r.Doctor?.FullName ?? string.Empty,
                    Status = r.Status
                })
                .ToList(),
            Form = new ConsultationFormViewModel
            {
                Id = record?.Id ?? 0,
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Symptoms = record?.Symptoms,
                Diagnosis = record?.Diagnosis,
                BloodPressure = record?.BloodPressure,
                Temperature = record?.Temperature,
                Pulse = record?.Pulse,
                Weight = record?.Weight,
                TreatmentPlan = record?.TreatmentPlan,
                Observations = record?.Observations,
                FollowUpRecommendations = record?.FollowUpRecommendations,
                Status = record?.Status ?? EntityStatus.RecordDraft
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ConsultationFormViewModel form)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == form.AppointmentId);
        if (appointment is null)
        {
            return NotFound();
        }

        if (!await IsOwnedByCurrentDoctorAsync(appointment))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var existingRecord = await _consultations.GetByAppointmentAsync(appointment.Id);
        if (!IsConsultationStatus(appointment.Status) && existingRecord?.Status != EntityStatus.RecordFinal)
        {
            return NotFound();
        }

        if (form.Id != 0 && (existingRecord is null || existingRecord.Id != form.Id))
        {
            return NotFound();
        }

        var record = new MedicalRecord
        {
            Id = existingRecord?.Id ?? 0,
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            Symptoms = form.Symptoms,
            Diagnosis = form.Diagnosis,
            BloodPressure = form.BloodPressure,
            Temperature = form.Temperature,
            Pulse = form.Pulse,
            Weight = form.Weight,
            TreatmentPlan = form.TreatmentPlan,
            Observations = form.Observations,
            FollowUpRecommendations = form.FollowUpRecommendations
        };

        var userName = appointment.Doctor?.User?.UserName
            ?? appointment.Doctor?.Email
            ?? User.Identity?.Name
            ?? string.Empty;

        try
        {
            await _consultations.SaveDraftAsync(record, userName);
        }
        catch (InvalidOperationException ex)
        {
            // Keep the form open with the typed values and the message beside the field.
            ModelState.AddModelError($"{nameof(ConsultationPageViewModel.Form)}.{nameof(ConsultationFormViewModel.Diagnosis)}", ex.Message);
            return await RenderPageAsync(form, record.Id);
        }

        TempData["Message"] = "Medical record saved.";
        return RedirectToAction(nameof(Index), new { appointmentId = form.AppointmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        var record = await _db.MedicalRecords
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null || record.Appointment is null)
        {
            return NotFound();
        }


        if (!await IsOwnedByCurrentDoctorAsync(record.Appointment))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!IsConsultationStatus(record.Appointment.Status))
        {
            return NotFound();
        }

        await _consultations.FinalizeAsync(id);
        TempData["Message"] = "Medical record finalized.";
        return RedirectToAction(nameof(Index), new { appointmentId = record.AppointmentId });
    }

    private async Task<IActionResult> RenderPageAsync(ConsultationFormViewModel form, int recordId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == form.AppointmentId);
        if (appointment is null || appointment.Patient is null)
        {
            return NotFound();
        }

        var history = await _consultations.GetRecentConsultationsAsync(appointment.PatientId, 5);

        var model = new ConsultationPageViewModel
        {
            AppointmentId = appointment.Id,
            AppointmentStatus = appointment.Status,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            PatientId = appointment.PatientId,
            PatientCode = appointment.Patient.PatientCode,
            PatientName = appointment.Patient.FullName,
            PatientGender = appointment.Patient.Gender,
            PatientDateOfBirth = appointment.Patient.DateOfBirth,
            PatientPhone = appointment.Patient.Phone,
            PatientEmail = appointment.Patient.Email,
            PatientAddress = appointment.Patient.Address,
            Allergies = appointment.Patient.Allergies,
            MedicalHistory = appointment.Patient.MedicalHistory,
            DoctorName = appointment.Doctor?.FullName ?? string.Empty,
            RecentConsultations = history
                .Select(r => new ConsultationHistoryItem
                {
                    MedicalRecordId = r.Id,
                    Date = r.CreatedAt,
                    Diagnosis = r.Diagnosis,
                    DoctorName = r.Doctor?.FullName ?? string.Empty,
                    Status = r.Status
                })
                .ToList(),
            Form = form
        };

        model.Form.Id = recordId;
        return View(nameof(Index), model);
    }

    private async Task<bool> IsOwnedByCurrentDoctorAsync(Appointment appointment)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return false;
        }

        return await _db.Doctors.AnyAsync(d => d.Id == appointment.DoctorId && d.UserId == user.Id);
    }

    private static bool IsConsultationStatus(string status)
    {
        return status == EntityStatus.AppointmentCheckedIn || status == EntityStatus.AppointmentInProgress;
    }
}
