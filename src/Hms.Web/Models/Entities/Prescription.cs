namespace Hms.Web.Models.Entities;

public class Prescription
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }

    public Doctor? Doctor { get; set; }

    public int? MedicalRecordId { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }

    public string Status { get; set; } = EntityStatus.PrescriptionActive;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
