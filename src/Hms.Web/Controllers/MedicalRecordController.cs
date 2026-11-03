using Hms.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Doctor + "," + Roles.Patient + "," + Roles.Admin)]
public class MedicalRecordController : Controller
{
    private readonly HmsDbContext _db;

    public MedicalRecordController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int patientId)
    {
        var records = await _db.MedicalRecords
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        ViewData["PatientId"] = patientId;
        return View(records);
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await _db.MedicalRecords
            .Include(r => r.Doctor)
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record is null)
        {
            return NotFound();
        }

        return View(record);
    }
}
