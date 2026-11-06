using Hospital.Application.Features.About;
using Hospital.Web.ViewModels.About;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[AllowAnonymous]
[Route("about")]
public sealed class AboutController : Controller
{
    private readonly IAboutContentQuery contentQuery;

    public AboutController(IAboutContentQuery contentQuery)
    {
        this.contentQuery = contentQuery;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var content = await contentQuery.GetAsync(cancellationToken);
        return View(AboutViewModel.From(content));
    }
}
