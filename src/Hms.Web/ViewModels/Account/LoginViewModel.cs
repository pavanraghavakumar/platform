using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Account;

/// <summary>
/// The sign-in form: a user name (or email) and a password, plus the page to return to after sign-in.
/// </summary>
public class LoginViewModel
{
    [Required]
    [Display(Name = "User name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
