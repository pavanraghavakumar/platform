namespace Hms.Web.ViewModels.Appointment;

/// <summary>The booking page: doctor and date pickers, the patient (Receptionist only) and the chosen slot.</summary>
public class BookAppointmentViewModel
{
    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public DateTime? Date { get; set; }

    public string? Time { get; set; }

    public string? Notes { get; set; }

    /// <summary>True when the signed-in person is a Receptionist who books for any patient.</summary>
    public bool CanChoosePatient { get; set; }

    /// <summary>The signed-in Patient's own record, shown instead of a patient picker.</summary>
    public Models.Entities.Patient? OwnPatient { get; set; }

    public IReadOnlyList<Models.Entities.Doctor> Doctors { get; set; } = Array.Empty<Models.Entities.Doctor>();

    public IReadOnlyList<Models.Entities.Patient> Patients { get; set; } = Array.Empty<Models.Entities.Patient>();
}
