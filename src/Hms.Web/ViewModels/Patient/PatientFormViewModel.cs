using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Patient;

/// <summary>
/// The registration form. Full name and phone are required; the controller shows the exact
/// message "Full name and phone number are required." when either is missing.
/// </summary>
public class PatientCreateViewModel
{
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Emergency contact name")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Medical history")]
    public string? MedicalHistory { get; set; }

    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }
}

/// <summary>The edit form: contact details and emergency contact only.</summary>
public class PatientEditViewModel
{
    public int Id { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Emergency contact name")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Medical history")]
    public string? MedicalHistory { get; set; }

    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }
}
