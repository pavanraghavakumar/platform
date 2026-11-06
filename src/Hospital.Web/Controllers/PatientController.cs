using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient")]
public sealed class PatientController : Controller
{
    [HttpGet("/patient")]
    public IActionResult Index() => View();
}
