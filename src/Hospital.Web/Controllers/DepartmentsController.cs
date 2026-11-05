using Hospital.Application.Features.Departments;
using Hospital.Web.ViewModels.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[AllowAnonymous]
public sealed class DepartmentsController : Controller
{
    private readonly IPublicDepartmentQuery departments;

    public DepartmentsController(IPublicDepartmentQuery departments)
    {
        this.departments = departments;
    }

    [HttpGet("/departments")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var published = await departments.ListPublishedAsync(cancellationToken);
        return View(new DepartmentIndexViewModel(published
            .Select(department => new DepartmentCardViewModel(
                department.Name,
                department.Slug,
                department.ImagePath,
                department.Description))
            .ToList()));
    }

    [HttpGet("/departments/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var department = await departments.FindPublishedBySlugAsync(slug, cancellationToken);
        if (department is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        return View(new DepartmentDetailsViewModel(
            department.Name,
            department.Slug,
            department.ImagePath,
            department.Description,
            department.Details,
            department.Doctors.Select(doctor => new DepartmentDoctorViewModel(
                doctor.Name,
                doctor.Slug,
                doctor.PhotoPath,
                doctor.Qualifications,
                doctor.Specialization)).ToList()));
    }
}
