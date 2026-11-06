using Hospital.Application.Features.PatientAuthentication;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Identity.PatientAuthentication;
using Hospital.Web.ViewModels.PatientAuthentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[AllowAnonymous]
public sealed class PatientAuthenticationController : Controller
{
    private const string PatientLanding = "/patient";
    private readonly IPatientAuthenticationService authentication;
    private readonly IPatientContinuationService continuations;
    private readonly UserManager<ApplicationUser> users;
    private readonly SignInManager<ApplicationUser> signIn;
    private readonly IServiceScopeFactory scopeFactory;

    public PatientAuthenticationController(
        IPatientAuthenticationService authentication,
        IPatientContinuationService continuations,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IServiceScopeFactory scopeFactory)
    {
        this.authentication = authentication;
        this.continuations = continuations;
        this.users = users;
        this.signIn = signIn;
        this.scopeFactory = scopeFactory;
    }

    [HttpGet("/patient/register")]
    public IActionResult Register([FromQuery] string? returnToken = null) =>
        View(new PatientRegistrationViewModel { ReturnToken = returnToken });

    [HttpPost("/patient/register")]
    public async Task<IActionResult> Register(
        PatientRegistrationViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ClearPasswords(model);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(model);
        }

        var result = await authentication.RegisterAsync(
            new PatientRegistrationCommand(
                model.Email,
                model.PatientIdentifier,
                model.Password,
                model.FullName,
                model.DateOfBirth,
                model.Phone),
            cancellationToken);

        if (result.IsSuccess)
        {
            var sessionIssued = await TryIssueSessionAsync(
                result.AccountId!,
                isPersistent: false);
            if (sessionIssued)
            {
                return SeeOther(ResolveDestination(model.ReturnToken));
            }

            if (!await TryRemoveCreatedAccountAsync(result.AccountId!))
            {
                return SeeOther(BuildRegistrationRecoveryDestination(model.ReturnToken));
            }

            result = new(PatientAuthenticationStatus.Unavailable);
        }

        AddRegistrationErrors(result);
        ClearPasswords(model);
        Response.StatusCode = result.Status == PatientAuthenticationStatus.Unavailable
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status400BadRequest;
        return View(model);
    }

    [HttpGet("/patient/login")]
    public IActionResult Login(
        [FromQuery] string? returnToken = null,
        [FromQuery] bool registrationCompleted = false) =>
        View(new PatientLoginViewModel
        {
            ReturnToken = returnToken,
            RegistrationCompleted = registrationCompleted
        });

    [HttpPost("/patient/login")]
    public async Task<IActionResult> Login(
        PatientLoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(model);
        }

        var result = await authentication.LoginAsync(
            new PatientLoginCommand(model.Identifier, model.Password, model.RememberMe),
            cancellationToken);

        if (result.IsSuccess)
        {
            var sessionIssued = await TryIssueSessionAsync(
                result.AccountId!,
                model.RememberMe);
            if (sessionIssued)
            {
                return SeeOther(ResolveDestination(model.ReturnToken));
            }

            result = new(PatientAuthenticationStatus.Unavailable);
        }

        ModelState.AddModelError(
            string.Empty,
            result.Status == PatientAuthenticationStatus.Unavailable
                ? IdentityPatientAuthenticationService.UnavailableMessage
                : IdentityPatientAuthenticationService.InvalidCredentialsMessage);
        model.Password = string.Empty;
        Response.StatusCode = result.Status == PatientAuthenticationStatus.Unavailable
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status400BadRequest;
        return View(model);
    }

    private void AddRegistrationErrors(PatientAuthenticationResult result)
    {
        if (result.Status == PatientAuthenticationStatus.ExistingAccount)
        {
            ModelState.AddModelError(string.Empty, IdentityPatientAuthenticationService.ExistingAccountMessage);
            return;
        }

        if (result.Status == PatientAuthenticationStatus.Unavailable)
        {
            ModelState.AddModelError(string.Empty, IdentityPatientAuthenticationService.UnavailableMessage);
            return;
        }

        if (result.Errors is null) return;
        foreach (var error in result.Errors)
        {
            foreach (var message in error.Value)
            {
                ModelState.AddModelError(error.Key, message);
            }
        }
    }

    private string ResolveDestination(string? token) =>
        string.IsNullOrWhiteSpace(token) ? PatientLanding : continuations.Consume(token) ?? PatientLanding;

    private static string BuildRegistrationRecoveryDestination(string? returnToken)
    {
        const string loginDestination = "/patient/login?registrationCompleted=true";
        return string.IsNullOrWhiteSpace(returnToken)
            ? loginDestination
            : $"{loginDestination}&returnToken={Uri.EscapeDataString(returnToken)}";
    }

    private async Task<bool> TryIssueSessionAsync(string accountId, bool isPersistent)
    {
        try
        {
            var user = await users.FindByIdAsync(accountId);
            if (user is null)
            {
                return false;
            }

            await signIn.SignInAsync(user, isPersistent);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<bool> TryRemoveCreatedAccountAsync(string accountId)
    {
        try
        {
            await using var cleanupScope = scopeFactory.CreateAsyncScope();
            var cleanupUsers = cleanupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await cleanupUsers.FindByIdAsync(accountId);
            if (user is null)
            {
                return true;
            }

            var deleted = await cleanupUsers.DeleteAsync(user);
            return deleted.Succeeded && await cleanupUsers.FindByIdAsync(accountId) is null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    private IActionResult SeeOther(string destination)
    {
        Response.StatusCode = StatusCodes.Status303SeeOther;
        Response.Headers.Location = destination;
        return new EmptyResult();
    }

    private static void ClearPasswords(PatientRegistrationViewModel model)
    {
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
    }
}
