using Microsoft.AspNetCore.Identity;

namespace Hms.Web.Models.Entities;

/// <summary>
/// The signed-in account. Every user holds exactly one of the four roles
/// Admin, Doctor, Receptionist or Patient.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }

    public Staff? Staff { get; set; }
}
