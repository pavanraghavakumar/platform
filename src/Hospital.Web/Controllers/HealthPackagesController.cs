using Hospital.Application.Features.HealthPackages;
using Hospital.Web.ViewModels.HealthPackages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[AllowAnonymous]
public sealed class HealthPackagesController : Controller
{
    private readonly IPublicHealthPackagesQuery packagesQuery;

    public HealthPackagesController(IPublicHealthPackagesQuery packagesQuery)
    {
        this.packagesQuery = packagesQuery;
    }

    [HttpGet("/health-packages")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var packages = await packagesQuery.GetPublishedAsync(cancellationToken);
        var model = new HealthPackagesIndexViewModel(packages
            .Select(package => new HealthPackageCardViewModel(
                package.Slug,
                package.Name,
                package.Description,
                package.Details))
            .ToArray());

        return View(model);
    }

    [HttpGet("/health-packages/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var package = await packagesQuery.GetPublishedBySlugAsync(slug, cancellationToken);
        if (package is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        return View(new HealthPackageDetailsViewModel(
            package.Slug,
            package.Name,
            package.Description,
            package.Details));
    }
}
