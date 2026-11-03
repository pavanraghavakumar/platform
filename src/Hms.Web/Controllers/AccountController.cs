using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("/")]
    [AllowAnonymous]
    public IActionResult Root()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserName)
            ?? await _userManager.FindByEmailAsync(model.UserName);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account is deactivated.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect(await DashboardUrlForAsync(user));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            // The reset link is shown on the page itself; nothing is emailed.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = model.Email, token },
                Request.Scheme);

            ViewData["ResetLink"] = link;
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        return View(new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Token = token ?? string.Empty
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Message"] = "Your password has been reset. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(await BuildProfileViewModelAsync(user));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileDetailsAsync(model, user);
            return View(model);
        }

        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), error.Description);
            }

            await PopulateProfileDetailsAsync(model, user);
            return View(model);
        }

        TempData["Message"] = "Profile saved.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        ViewData["Message"] = "Your password has been changed.";
        return View(new ChangePasswordViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user)
    {
        var model = new ProfileViewModel { PhoneNumber = user.PhoneNumber };
        await PopulateProfileDetailsAsync(model, user);
        return model;
    }

    private async Task PopulateProfileDetailsAsync(ProfileViewModel model, ApplicationUser user)
    {
        model.UserName = user.UserName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;
        model.Role = (await _userManager.GetRolesAsync(user)).SingleOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// The dashboard of the signed-in person's role: Admin, Doctor, Receptionist or Patient.
    /// </summary>
    private async Task<string> DashboardUrlForAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return Url.Action("Admin", "Dashboard") ?? "/Dashboard/Admin";
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Doctor))
        {
            return Url.Action("Doctor", "Dashboard") ?? "/Dashboard/Doctor";
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Receptionist))
        {
            return Url.Action("Receptionist", "Dashboard") ?? "/Dashboard/Receptionist";
        }

        return Url.Action("Patient", "Dashboard") ?? "/Dashboard/Patient";
    }
}
