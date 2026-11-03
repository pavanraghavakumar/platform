using Hms.Web.Data;
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
    private readonly HmsDbContext _db;

    public DashboardController(
        UserManager<Models.Entities.ApplicationUser> userManager,
        IReportService reportService,
        HmsDbContext db)
    {
        _userManager = userManager;
        _reportService = reportService;
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
    public IActionResult Receptionist() => View();

    [Authorize(Roles = Roles.Patient)]
    public IActionResult Patient() => View();
}
