using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Account;

/// <summary>
/// The forgot-password form. The reset link is shown on the page itself, never emailed.
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
