using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Department;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DepartmentController : Controller
{
    private const int PageSize = 10;

    private readonly HmsDbContext _db;

    public DepartmentController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? status, int page = 1)
    {
        var query = _db.Departments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        query = sort switch
        {
            "name_desc" => query.OrderByDescending(d => d.Name),
            "status" => query.OrderBy(d => d.Status).ThenBy(d => d.Name),
            _ => query.OrderBy(d => d.Name)
        };

        var total = await query.CountAsync();
        var currentPage = page < 1 ? 1 : page;

        var rows = await query
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(d => new DepartmentRow
            {
                Id = d.Id,
                Name = d.Name,
                Status = d.Status,
                DoctorCount = d.Doctors.Count,
                StaffCount = d.Staff.Count
            })
            .ToListAsync();

        return View(new DepartmentListViewModel
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
    public IActionResult Create() => View(new DepartmentCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentCreateViewModel model)
    {
        var name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(model.Name), "Name is required.");
        }

        if (ModelState.IsValid && await _db.Departments.AnyAsync(d => d.Name == name))
        {
            ModelState.AddModelError(nameof(model.Name), "A department with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var now = DateTime.UtcNow;
        _db.Departments.Add(new Department
        {
            Name = name,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "Department created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department is not null)
        {
            // Only the status changes: the department's doctors and appointments stay as they are.
            department.Status = EntityStatus.Inactive;
            department.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Department deactivated.";
        }

        return RedirectToAction(nameof(Index));
    }
}
