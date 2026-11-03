using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Patient;

/// <summary>
/// The patient profile: personal and contact details, emergency contact, medical history,
/// allergies, status and the appointment history newest first.
/// </summary>
public class PatientDetailsViewModel
{
    public Models.Entities.Patient Patient { get; set; } = new();

    public IReadOnlyList<Models.Entities.Appointment> Appointments { get; set; } = Array.Empty<Models.Entities.Appointment>();

    public int MedicalRecordCount { get; set; }

    public int InvoiceCount { get; set; }
}
