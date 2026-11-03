using Hms.Web.Data;
using Hms.Web.Services;
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

    public IActionResult Index() => View();

    public IActionResult Patients() => View();

    public IActionResult Appointments() => View();

    public IActionResult Departments() => View();

    public IActionResult Doctors() => View();

    public IActionResult Laboratory() => View();

    public IActionResult Pharmacy() => View();

    public IActionResult Revenue() => View();
}
