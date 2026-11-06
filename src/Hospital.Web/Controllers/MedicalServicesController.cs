using Hospital.Application.Features.MedicalServices;
using Hospital.Web.ViewModels.MedicalServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[AllowAnonymous]
[Route("medical-services")]
public sealed class MedicalServicesController : Controller
{
    private readonly IMedicalServicesQuery query;

    public MedicalServicesController(IMedicalServicesQuery query)
    {
        this.query = query;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await query.ListAsync(cancellationToken);
        return View(new MedicalServicesIndexViewModel(services.Select(ToCard).ToArray()));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var service = await query.FindAsync(slug, cancellationToken);
        if (service is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", slug);
        }

        return View(new MedicalServiceDetailsViewModel(
            service.Slug, service.Name, service.Description, MedicalServiceImages.Resolve(service.ImagePath),
            MedicalServiceImages.SourceSet(service.ImagePath),
            $"Healthcare professionals supporting {service.Name}", service.Details, service.CareInformation));
    }

    private static MedicalServiceCardViewModel ToCard(MedicalServiceSummary service) =>
        new(service.Slug, service.Name, service.Description, MedicalServiceImages.Resolve(service.ImagePath),
            MedicalServiceImages.SourceSet(service.ImagePath), $"Healthcare service: {service.Name}");
}

