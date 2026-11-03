using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Account;

public class ProfileViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [Display(Name = "Phone number")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? PhoneNumber { get; set; }
}
