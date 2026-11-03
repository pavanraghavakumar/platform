using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize]
public class LabRequestController : Controller
{
    private readonly HmsDbContext _db;
    private readonly ILabService _lab;

    public LabRequestController(HmsDbContext db, ILabService lab)
    {
        _db = db;
        _lab = lab;
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    public async Task<IActionResult> Index(string? status)
    {
        var query = _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.LaboratoryTest)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        ViewData["Status"] = status;
        return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int patientId, int doctorId, int? appointmentId, int[] testIds)
    {
        await _lab.CreateRequestsAsync(patientId, doctorId, appointmentId, testIds ?? Array.Empty<int>());
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        await _lab.StartAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var request = await _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.LaboratoryTest)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string resultValue, string? remarks)
    {
        await _lab.CompleteAsync(id, resultValue, remarks);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Patient + "," + Roles.Admin)]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.LaboratoryTest)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Patient)]
    public async Task<IActionResult> Print(int id)
    {
        var request = await _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.LaboratoryTest)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }
}
