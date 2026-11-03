using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DoctorController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IDoctorService _doctors;

    public DoctorController(HmsDbContext db, IDoctorService doctors)
    {
        _db = db;
        _doctors = doctors;
    }

    public async Task<IActionResult> Index(int? departmentId)
    {
        var query = _db.Doctors.AsNoTracking().AsQueryable();
        if (departmentId is not null)
        {
            query = query.Where(d => d.DepartmentId == departmentId);
        }

        return View(new DoctorListViewModel
        {
            DepartmentId = departmentId,
            Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name)
                .Select(d => new DoctorDepartmentOption { Id = d.Id, Name = d.Name }).ToListAsync(),
            Rows = await query.OrderBy(d => d.FullName).Select(d => new DoctorListRow
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialization = d.Specialization,
                DepartmentName = d.Department!.Name,
                ConsultationFee = d.ConsultationFee,
                Status = d.Status
            }).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(new DoctorCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorCreateViewModel model)
    {
        if (!await _db.Departments.AnyAsync(d => d.Id == model.DepartmentId))
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Choose a department.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync();
            return View(model);
        }

        try
        {
            await _doctors.CreateAsync(model);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.Email), exception.Message);
            await LoadDepartmentsAsync();
            return View(model);
        }

        TempData["Message"] = "Doctor created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _doctors.GetEditAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        await LoadDepartmentsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DoctorEditViewModel model)
    {
        model.Id = id;
        for (var index = 0; index < model.Schedules.Count; index++)
        {
            var schedule = model.Schedules[index];
            if (schedule.IsWorking && (schedule.StartTime is null || schedule.EndTime is null || schedule.EndTime <= schedule.StartTime))
            {
                ModelState.AddModelError($"Schedules[{index}].EndTime", "End time must be after start time.");
            }
        }

        if (!await _db.Departments.AnyAsync(d => d.Id == model.DepartmentId))
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Choose a department.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync();
            return View(model);
        }

        try
        {
            if (!await _doctors.UpdateAsync(model))
            {
                return NotFound();
            }
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.Email), exception.Message);
            await LoadDepartmentsAsync();
            return View(model);
        }

        TempData["Message"] = "Doctor details saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _db.Doctors.AsNoTracking().Include(d => d.Department).Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return NotFound();
        }

        var appointments = await _db.Appointments.AsNoTracking().Where(a => a.DoctorId == id)
            .Select(a => new DoctorAppointmentHistoryRow
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                PatientName = a.Patient!.FullName,
                Status = a.Status
            }).ToListAsync();

        return View(new DoctorDetailsViewModel
        {
            Id = doctor.Id,
            FullName = doctor.FullName,
            Email = doctor.Email,
            Phone = doctor.Phone,
            Specialization = doctor.Specialization,
            DepartmentName = doctor.Department?.Name ?? string.Empty,
            Qualifications = doctor.Qualifications ?? string.Empty,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            Status = doctor.Status,
            Schedules = doctor.Schedules.OrderBy(s => s.DayOfWeek).Select(s => new DoctorScheduleInput
            {
                DayOfWeek = s.DayOfWeek,
                IsWorking = s.IsWorking,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList(),
            Appointments = appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (await _doctors.DeactivateAsync(id))
        {
            TempData["Message"] = "Doctor deactivated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDepartmentsAsync()
    {
        ViewData["Departments"] = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
    }
}
