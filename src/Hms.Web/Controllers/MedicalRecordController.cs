using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.MedicalRecord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Doctor + "," + Roles.Patient + "," + Roles.Admin)]
public class MedicalRecordController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IConsultationService _consultations;
    private readonly UserManager<ApplicationUser> _users;

    public MedicalRecordController(HmsDbContext db, IConsultationService consultations, UserManager<ApplicationUser> users)
    {
        _db = db;
        _consultations = consultations;
        _users = users;
    }

    public async Task<IActionResult> Index(int patientId)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient is null)
        {
            return NotFound();
        }

        if (!await CanAccessPatientAsync(patient))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var records = await _db.MedicalRecords
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var model = new MedicalRecordListViewModel
        {
            PatientId = patient.Id,
            PatientCode = patient.PatientCode,
            PatientName = patient.FullName,
            Records = records
                .Select(r => new MedicalRecordRow
                {
                    Id = r.Id,
                    Date = r.CreatedAt,
                    DoctorName = r.Doctor?.FullName ?? string.Empty,
                    Diagnosis = r.Diagnosis,
                    Status = r.Status
                })
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await _db.MedicalRecords
            .Include(r => r.Doctor)
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null || record.Patient is null)
        {
            return NotFound();
        }

        if (!await CanAccessPatientAsync(record.Patient))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = new MedicalRecordDetailsViewModel
        {
            Id = record.Id,
            PatientId = record.PatientId,
            PatientCode = record.Patient.PatientCode,
            PatientName = record.Patient.FullName,
            DoctorName = record.Doctor?.FullName ?? string.Empty,
            Date = record.CreatedAt,
            Symptoms = record.Symptoms,
            Diagnosis = record.Diagnosis,
            BloodPressure = record.BloodPressure,
            Temperature = record.Temperature,
            Pulse = record.Pulse,
            Weight = record.Weight,
            TreatmentPlan = record.TreatmentPlan,
            Observations = record.Observations,
            FollowUpRecommendations = record.FollowUpRecommendations,
            Status = record.Status
        };

        return View(model);
    }

    /// <summary>
    /// A Doctor may open the records of a patient they have an appointment with; a Patient may
    /// open only their own; an Admin may open any.
    /// </summary>
    private async Task<bool> CanAccessPatientAsync(Patient patient)
    {
        if (User.IsInRole(Roles.Admin))
        {
            return true;
        }

        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return false;
        }

        if (User.IsInRole(Roles.Patient))
        {
            return patient.UserId == user.Id;
        }

        if (User.IsInRole(Roles.Doctor))
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor is null)
            {
                return false;
            }

            return await _consultations.DoctorHasAppointmentWithPatientAsync(doctor.Id, patient.Id);
        }

        return false;
    }
}
