namespace Hms.Web.Models.Entities;

public class Patient
{
    public int Id { get; set; }

    /// <summary>Patient ID of the form <c>P-00001</c>, unique.</summary>
    public string PatientCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? MedicalHistory { get; set; }

    public string? Allergies { get; set; }

    public string Status { get; set; } = EntityStatus.Active;

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
