using System.ComponentModel.DataAnnotations;

namespace Hospital.Web.ViewModels.PatientAuthentication;

public sealed class PatientLoginViewModel
{
    public bool RegistrationCompleted { get; set; }

    [Required(ErrorMessage = "Enter your email address or patient identifier.")]
    [Display(Name = "Email address or patient identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnToken { get; set; }
}
