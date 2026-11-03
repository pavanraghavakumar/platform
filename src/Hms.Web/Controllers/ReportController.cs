using Hms.Web.Data;
using Hms.Web.Services;
using Hms.Web.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ReportController : Controller
{
    private readonly IReportService _reports;

    public ReportController(IReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to) => View(await _reports.GetReportsAsync(from, to));

    public async Task<IActionResult> Patients(DateTime? from, DateTime? to) => View(await _reports.GetPatientReportAsync(ReportPeriod.Create(from, to)));

    public async Task<IActionResult> Appointments(DateTime? from, DateTime? to) => View(await _reports.GetAppointmentReportAsync(ReportPeriod.Create(from, to)));

    public async Task<IActionResult> Departments(DateTime? from, DateTime? to) => View(await _reports.GetDepartmentReportAsync(ReportPeriod.Create(from, to)));

    public async Task<IActionResult> Doctors(DateTime? from, DateTime? to) => View(await _reports.GetDoctorReportAsync(ReportPeriod.Create(from, to)));

    public async Task<IActionResult> Laboratory(DateTime? from, DateTime? to) => View(await _reports.GetLaboratoryReportAsync(ReportPeriod.Create(from, to)));

    public async Task<IActionResult> Pharmacy() => View(await _reports.GetPharmacyReportAsync());

    public async Task<IActionResult> Revenue(DateTime? from, DateTime? to) => View(await _reports.GetRevenueReportAsync(ReportPeriod.Create(from, to)));
}
