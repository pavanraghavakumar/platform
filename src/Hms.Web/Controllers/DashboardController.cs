using Hms.Web.Data;
using Hms.Web.Services;
using Hms.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<Models.Entities.ApplicationUser> _userManager;
    private readonly IReportService _reportService;

    public DashboardController(
        UserManager<Models.Entities.ApplicationUser> userManager,
        IReportService reportService)
    {
        _userManager = userManager;
        _reportService = reportService;
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
    public IActionResult Doctor() => View();

    [Authorize(Roles = Roles.Receptionist)]
    public IActionResult Receptionist() => View();

    [Authorize(Roles = Roles.Patient)]
    public IActionResult Patient() => View();
}
