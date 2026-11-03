using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Prescription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Doctor + "," + Roles.Patient)]
public class PrescriptionController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IPrescriptionService _prescriptions;
    private readonly UserManager<ApplicationUser> _users;

    public PrescriptionController(HmsDbContext db, IPrescriptionService prescriptions, UserManager<ApplicationUser> users)
    {
        _db = db;
        _prescriptions = prescriptions;
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

        var prescriptions = await _prescriptions.GetHistoryAsync(patientId);
        return View(new PrescriptionHistoryViewModel
        {
            PatientId = patient.Id,
            PatientCode = patient.PatientCode,
            PatientName = patient.FullName,
            Prescriptions = prescriptions.Select(p => new PrescriptionHistoryRow
            {
                Id = p.Id,
                Date = p.CreatedAt,
                DoctorName = p.Doctor?.FullName ?? string.Empty,
                MedicineCount = p.Items.Count,
                Status = p.Status
            }).ToList()
        });
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpGet]
    public async Task<IActionResult> Create(int appointmentId)
    {
        var appointment = await GetAppointmentAsync(appointmentId);
        if (appointment is null || appointment.Patient is null || appointment.MedicalRecord is null)
        {
            return NotFound();
        }

        if (!await IsOwnedByCurrentDoctorAsync(appointment))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(await BuildCreateModelAsync(appointment));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrescriptionCreateViewModel model)
    {
        var appointment = await GetAppointmentAsync(model.AppointmentId);
        if (appointment is null || appointment.Patient is null || appointment.MedicalRecord is null)
        {
            return NotFound();
        }

        if (!await IsOwnedByCurrentDoctorAsync(appointment))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        model.Items ??= new List<PrescriptionLineViewModel>();
        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Items), "Add at least one medicine.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateModelAsync(model, appointment);
            return View(model);
        }

        var prescription = new Prescription
        {
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            MedicalRecordId = appointment.MedicalRecord.Id,
            Notes = model.Notes,
            Items = model.Items.Select(item => new PrescriptionItem
            {
                MedicineId = item.MedicineId,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Instructions = item.Instructions
            }).ToList()
        };

        try
        {
            await _prescriptions.CreateAsync(prescription);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCreateModelAsync(model, appointment);
            return View(model);
        }

        TempData["Message"] = "Prescription created.";
        return RedirectToAction(nameof(Print), new { id = prescription.Id });
    }

    public async Task<IActionResult> Print(int id)
    {
        var prescription = await _prescriptions.GetPrintableAsync(id);
        if (prescription is null || prescription.Patient is null)
        {
            return NotFound();
        }

        if (!await CanAccessPatientAsync(prescription.Patient))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(new PrescriptionPrintViewModel
        {
            Id = prescription.Id,
            PatientId = prescription.PatientId,
            PatientCode = prescription.Patient.PatientCode,
            PatientName = prescription.Patient.FullName,
            DoctorName = prescription.Doctor?.FullName ?? string.Empty,
            Date = prescription.CreatedAt,
            Status = prescription.Status,
            Notes = prescription.Notes,
            Items = prescription.Items.Select(item => new PrescriptionPrintLine
            {
                MedicineName = item.Medicine?.Name ?? string.Empty,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Instructions = item.Instructions
            }).ToList()
        });
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var prescription = await _db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id);
        if (prescription is null)
        {
            return NotFound();
        }

        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null || prescription.DoctorId != doctor.Id)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        try
        {
            await _prescriptions.SetStatusAsync(id, status, User.Identity?.Name ?? doctor.Email);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Print), new { id });
    }

    private async Task<Appointment?> GetAppointmentAsync(int appointmentId)
    {
        return await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.MedicalRecord)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
    }

    private async Task<PrescriptionCreateViewModel> BuildCreateModelAsync(Appointment appointment)
    {
        var model = new PrescriptionCreateViewModel();
        await PopulateCreateModelAsync(model, appointment);
        return model;
    }

    private async Task PopulateCreateModelAsync(PrescriptionCreateViewModel model, Appointment appointment)
    {
        model.AppointmentId = appointment.Id;
        model.PatientId = appointment.PatientId;
        model.PatientCode = appointment.Patient?.PatientCode ?? string.Empty;
        model.PatientName = appointment.Patient?.FullName ?? string.Empty;
        model.DoctorName = appointment.Doctor?.FullName ?? string.Empty;
        model.MedicalRecordId = appointment.MedicalRecord?.Id ?? 0;
        model.Medicines = await _db.Medicines
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new PrescriptionMedicineOption { Id = m.Id, Name = m.Name })
            .ToListAsync();
    }

    private async Task<bool> IsOwnedByCurrentDoctorAsync(Appointment appointment)
    {
        var doctor = await GetCurrentDoctorAsync();
        return doctor is not null && doctor.Id == appointment.DoctorId;
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var user = await _users.GetUserAsync(User);
        return user is null ? null : await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
    }

    private async Task<bool> CanAccessPatientAsync(Patient patient)
    {
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
            return doctor is not null && await _db.Appointments.AnyAsync(a =>
                a.DoctorId == doctor.Id && a.PatientId == patient.Id);
        }

        return false;
    }
}
