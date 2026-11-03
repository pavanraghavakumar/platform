using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class StaffController : Controller
{
    private const int PageSize = 10;
    private static readonly string[] AllowedRoles = { Roles.Receptionist, Roles.Admin };

    private readonly HmsDbContext _db;
    private readonly IStaffService _staffService;

    public StaffController(HmsDbContext db, IStaffService staffService)
    {
        _db = db;
        _staffService = staffService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? status, int page = 1)
    {
        var query = _db.Staff.Include(s => s.Department).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.FullName.Contains(term) ||
                s.Email.Contains(term) ||
                s.Phone.Contains(term) ||
                s.Designation.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        query = sort switch
        {
            "name_desc" => query.OrderByDescending(s => s.FullName),
            "role" => query.OrderBy(s => s.Role).ThenBy(s => s.FullName),
            "status" => query.OrderBy(s => s.Status).ThenBy(s => s.FullName),
            _ => query.OrderBy(s => s.FullName)
        };

        var total = await query.CountAsync();
        var currentPage = page < 1 ? 1 : page;

        var rows = await query
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(s => new StaffRow
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                Role = s.Role,
                Designation = s.Designation,
                DepartmentName = s.Department != null ? s.Department.Name : null,
                Status = s.Status
            })
            .ToListAsync();

        return View(new StaffListViewModel
        {
            Rows = rows,
            Search = search,
            Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            Status = status,
            Page = currentPage,
            PageSize = PageSize,
            TotalCount = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDepartmentsAsync();
        return View(new StaffCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffCreateViewModel model)
    {
        if (!AllowedRoles.Contains(model.Role, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose a valid role.");
        }

        if (model.DepartmentId is not null && !await _db.Departments.AnyAsync(d => d.Id == model.DepartmentId))
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Choose an existing department.");
        }

        if (ModelState.IsValid && await _db.Staff.AnyAsync(s => s.Email == model.Email.Trim()))
        {
            ModelState.AddModelError(nameof(model.Email), "A staff member with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync();
            return View(model);
        }

        var staff = new Staff
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone.Trim(),
            Role = model.Role,
            Designation = model.Designation.Trim(),
            DepartmentId = model.DepartmentId
        };

        try
        {
            await _staffService.CreateAsync(staff, model.Password);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDepartmentsAsync();
            return View(model);
        }

        TempData["Message"] = "Staff member created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (await _staffService.DeactivateAsync(id))
        {
            TempData["Message"] = "Staff member deactivated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDepartmentsAsync()
    {
        ViewData["Departments"] = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
    }
}
