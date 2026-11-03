using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Staff;

/// <summary>
/// The New staff member form. Saving it creates the staff record with status Active
/// and a sign-in account that holds the chosen role.
/// </summary>
public class StaffCreateViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200, ErrorMessage = "Full name must be 200 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [StringLength(30, ErrorMessage = "Phone must be 30 characters or fewer.")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation is required.")]
    [StringLength(150, ErrorMessage = "Designation must be 150 characters or fewer.")]
    [Display(Name = "Designation")]
    public string Designation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}
