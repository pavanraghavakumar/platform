using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public PrescriptionController(HmsDbContext db, IPrescriptionService prescriptions, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _prescriptions = prescriptions;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int patientId)
    {
        var prescriptions = await _db.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        ViewData["PatientId"] = patientId;
        return View(prescriptions);
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpGet]
    public async Task<IActionResult> Create(int appointmentId)
    {
        ViewData["Medicines"] = await _db.Medicines.OrderBy(m => m.Name).ToListAsync();
        ViewData["AppointmentId"] = appointmentId;
        return View(new Prescription());
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Prescription prescription)
    {
        try
        {
            await _prescriptions.CreateAsync(prescription);
            return RedirectToAction(nameof(Index), new { patientId = prescription.PatientId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Medicines"] = await _db.Medicines.OrderBy(m => m.Name).ToListAsync();
            return View(prescription);
        }
    }

    public async Task<IActionResult> Print(int id)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items).ThenInclude(i => i.Medicine)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prescription is null)
        {
            return NotFound();
        }

        return View(prescription);
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var user = await _userManager.GetUserAsync(User);
        await _prescriptions.SetStatusAsync(id, status, user?.UserName ?? string.Empty);
        return RedirectToAction("Index", "Dashboard");
    }
}
