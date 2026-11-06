using System.ComponentModel.DataAnnotations;

namespace Hospital.Web.ViewModels.PatientAuthentication;

public sealed class PatientRegistrationViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your patient identifier.")]
    [Display(Name = "Patient identifier")]
    [RegularExpression("^[A-Za-z0-9-]{4,32}$", ErrorMessage = "Use 4 to 32 letters, numbers, or hyphens.")]
    public string PatientIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [MinLength(12, ErrorMessage = "Use at least 12 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your full name.")]
    [Display(Name = "Full name")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Enter your phone number.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? ReturnToken { get; set; }
}
