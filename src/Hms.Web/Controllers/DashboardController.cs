using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<Models.Entities.ApplicationUser> _userManager;
    private readonly IReportService _reportService;
    private readonly IAppointmentService _appointmentService;
    private readonly HmsDbContext _db;

    public DashboardController(
        UserManager<Models.Entities.ApplicationUser> userManager,
        IReportService reportService,
        IAppointmentService appointmentService,
        HmsDbContext db)
    {
        _userManager = userManager;
        _reportService = reportService;
        _appointmentService = appointmentService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Doctor))
        {
            return RedirectToAction(nameof(Doctor));
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Receptionist))
        {
            return RedirectToAction(nameof(Receptionist));
        }

        return RedirectToAction(nameof(Patient));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Admin()
    {
        var data = await _reportService.GetAdminDashboardAsync(DateTime.UtcNow);
        return View(AdminDashboardViewModel.FromData(data));
    }

    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> Doctor()
    {
        var userId = _userManager.GetUserId(User);
        var doctorId = await _db.Doctors
            .Where(d => d.UserId == userId)
            .Select(d => (int?)d.Id)
            .SingleOrDefaultAsync();

        if (doctorId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var appointments = _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId.Value);

        var todayAppointments = await appointments
            .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
            .Select(a => new DoctorAppointmentRow
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                PatientName = a.Patient!.FullName,
                PatientCode = a.Patient.PatientCode,
                Status = a.Status
            })
            .ToListAsync();

        var upcomingAppointments = await appointments
            .Where(a => a.AppointmentDate >= tomorrow)
            .Select(a => new DoctorAppointmentRow
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                PatientName = a.Patient!.FullName,
                PatientCode = a.Patient.PatientCode,
                Status = a.Status
            })
            .ToListAsync();

        var model = new DoctorDashboardViewModel
        {
            TodaysAppointments = await appointments.CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow),
            CheckedInWaiting = await appointments.CountAsync(a => a.AppointmentDate >= today
                && a.AppointmentDate < tomorrow
                && a.Status == Models.Entities.EntityStatus.AppointmentCheckedIn),
            CompletedConsultationsToday = await _db.MedicalRecords.CountAsync(r => r.DoctorId == doctorId.Value
                && r.Status == Models.Entities.EntityStatus.RecordFinal
                && r.UpdatedAt >= today
                && r.UpdatedAt < tomorrow),
            LabResultsCompletedToday = await _db.LabRequests.CountAsync(r => r.DoctorId == doctorId.Value
                && r.Status == Models.Entities.EntityStatus.LabCompleted
                && r.ResultEnteredAt >= today
                && r.ResultEnteredAt < tomorrow),
            Today = todayAppointments
                .OrderBy(a => a.StartTime)
                .ThenBy(a => a.Id)
                .ToList(),
            Upcoming = upcomingAppointments
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.Id)
                .Take(5)
                .ToList(),
            RecentNotifications = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(5)
                .Select(n => new DoctorNotificationRow
                {
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync()
        };

        return View(model);
    }

    [Authorize(Roles = Roles.Receptionist)]
    public async Task<IActionResult> Receptionist()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var appointments = _db.Appointments.AsNoTracking();

        var todayAppointments = await appointments
            .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
            .Select(a => new ReceptionistAppointmentRow
            {
                Id = a.Id,
                StartTime = a.StartTime,
                PatientName = a.Patient!.FullName,
                DoctorName = a.Doctor!.FullName,
                Status = a.Status
            })
            .ToListAsync();

        var recordedPayments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.Status == Models.Entities.EntityStatus.PaymentRecorded
                && p.PaidAt >= today
                && p.PaidAt < tomorrow)
            .Select(p => p.Amount)
            .ToListAsync();

        var patientChoices = await _db.Patients
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .ThenBy(p => p.PatientCode)
            .Select(p => new ReceptionistPatientChoice
            {
                Id = p.Id,
                PatientCode = p.PatientCode,
                FullName = p.FullName,
                Phone = p.Phone,
                InvoiceAppointmentId = p.Appointments
                    .Where(a => a.Invoice == null)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.Id)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var model = new ReceptionistDashboardViewModel
        {
            TodaysAppointments = todayAppointments.Count,
            PendingAppointments = await appointments.CountAsync(a => a.Status == Models.Entities.EntityStatus.AppointmentPending),
            CheckedInAppointments = await appointments.CountAsync(a => a.Status == Models.Entities.EntityStatus.AppointmentCheckedIn),
            PaymentsRecordedToday = recordedPayments.Sum(),
            Today = todayAppointments
                .OrderBy(a => a.StartTime)
                .ThenBy(a => a.Id)
                .ToList(),
            Patients = patientChoices,
            RecentNotifications = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(5)
                .Select(n => new ReceptionistNotificationRow
                {
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync()
        };

        return View(model);
    }

    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> Patient()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var now = DateTime.UtcNow;
        var today = now.Date;
        var upcoming = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patient.Id
                && a.AppointmentDate >= today
                && a.Status != EntityStatus.AppointmentCancelled
                && a.Status != EntityStatus.AppointmentNoShow
                && a.Status != EntityStatus.AppointmentCompleted)
            .Select(a => new PatientAppointmentRow
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                DoctorName = a.Doctor!.FullName,
                Status = a.Status
            })
            .ToListAsync();

        upcoming = upcoming
            .Where(a => a.AppointmentDate.Date.Add(a.StartTime) > now)
            .ToList();

        var openInvoices = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.PatientId == patient.Id
                && i.Status != EntityStatus.InvoicePaid
                && i.Status != EntityStatus.InvoiceCancelled)
            .Select(i => new { i.Total, i.PaidAmount })
            .ToListAsync();

        var model = new PatientDashboardViewModel
        {
            NextAppointment = upcoming
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.Id)
                .FirstOrDefault(),
            UpcomingAppointments = upcoming.Count,
            UnpaidInvoices = openInvoices.Count,
            OutstandingBalance = openInvoices.Sum(i => Math.Max(0, i.Total - i.PaidAmount)),
            RecentNotifications = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == patient.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(5)
                .Select(n => new PatientNotificationRow
                {
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync()
        };

        return View(model);
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Book")]
    public async Task<IActionResult> PortalBook() => View("~/Views/Portal/Book.cshtml", await BuildBookingModelAsync());

    [Authorize(Roles = Roles.Patient)]
    [HttpPost("Portal/Book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortalBook(PatientBookingViewModel model)
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!TimeSpan.TryParse(model.Time, out var startTime))
        {
            ModelState.AddModelError(nameof(model.Time), "Choose a time slot.");
        }

        if (!ModelState.IsValid)
        {
            model.Doctors = (await BuildBookingModelAsync()).Doctors;
            return View("~/Views/Portal/Book.cshtml", model);
        }

        try
        {
            await _appointmentService.BookAsync(patient.Id, model.DoctorId, model.Date, startTime, null);
            TempData["Message"] = "Appointment booked.";
            return RedirectToAction(nameof(PortalAppointments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Doctors = (await BuildBookingModelAsync()).Doctors;
            return View("~/Views/Portal/Book.cshtml", model);
        }
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Slots")]
    public async Task<IActionResult> PortalSlots(int doctorId, DateTime date) =>
        Json(new { slots = await _appointmentService.GetFreeSlotsAsync(doctorId, date) });

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Appointments")]
    public async Task<IActionResult> PortalAppointments()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patient.Id)
            .Select(a => new PatientAppointmentRow
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                DoctorName = a.Doctor!.FullName,
                Status = a.Status
            })
            .ToListAsync();

        return View("~/Views/Portal/Appointments.cshtml", appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ThenByDescending(a => a.Id)
            .ToList());
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpPost("Portal/Appointments/{id:int}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortalCancel(int id)
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var appointment = await _db.Appointments
            .SingleOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
        if (appointment is not null
            && (appointment.Status == EntityStatus.AppointmentPending
                || appointment.Status == EntityStatus.AppointmentConfirmed))
        {
            appointment.Status = EntityStatus.AppointmentCancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Appointment cancelled.";
        }

        return RedirectToAction(nameof(PortalAppointments));
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Records")]
    public async Task<IActionResult> PortalRecords()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var records = await _db.MedicalRecords
            .AsNoTracking()
            .Where(r => r.PatientId == patient.Id && r.Status == EntityStatus.RecordFinal)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new PatientRecordRow
            {
                Id = r.Id,
                Date = r.CreatedAt,
                DoctorName = r.Doctor!.FullName,
                Diagnosis = r.Diagnosis ?? string.Empty
            })
            .ToListAsync();
        return View("~/Views/Portal/Records.cshtml", records);
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Prescriptions")]
    public async Task<IActionResult> PortalPrescriptions()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var rows = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patient.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PatientPrescriptionRow
            {
                Id = p.Id,
                Date = p.CreatedAt,
                DoctorName = p.Doctor!.FullName,
                MedicineCount = p.Items.Count,
                Status = p.Status
            })
            .ToListAsync();
        return View("~/Views/Portal/Prescriptions.cshtml", rows);
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/LabReports")]
    public async Task<IActionResult> PortalLabReports()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var rows = await _db.LabRequests
            .AsNoTracking()
            .Where(r => r.PatientId == patient.Id && r.Status == EntityStatus.LabCompleted)
            .OrderByDescending(r => r.ResultEnteredAt ?? r.UpdatedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new PatientLabReportRow
            {
                Id = r.Id,
                Date = r.ResultEnteredAt ?? r.UpdatedAt,
                TestName = r.LaboratoryTest!.Name,
                DoctorName = r.Doctor!.FullName
            })
            .ToListAsync();
        return View("~/Views/Portal/LabReports.cshtml", rows);
    }

    [Authorize(Roles = Roles.Patient)]
    [HttpGet("Portal/Invoices")]
    public async Task<IActionResult> PortalInvoices()
    {
        var patient = await CurrentPatientAsync();
        if (patient is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var rows = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.PatientId == patient.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .Select(i => new PatientInvoiceRow
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.CreatedAt,
                Total = i.Total,
                PaidAmount = i.PaidAmount,
                Status = i.Status
            })
            .ToListAsync();
        return View("~/Views/Portal/Invoices.cshtml", rows);
    }

    private async Task<Patient?> CurrentPatientAsync()
    {
        var userId = _userManager.GetUserId(User);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId);
    }

    private async Task<PatientBookingViewModel> BuildBookingModelAsync()
    {
        return new PatientBookingViewModel
        {
            Date = DateTime.UtcNow.Date,
            Doctors = await _db.Doctors
                .AsNoTracking()
                .Where(d => d.Status == EntityStatus.Active)
                .OrderBy(d => d.FullName)
                .Select(d => new PatientDoctorRow
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    Specialization = d.Specialization,
                    DepartmentName = d.Department!.Name,
                    ConsultationFee = d.ConsultationFee
                })
                .ToListAsync()
        };
    }
}
