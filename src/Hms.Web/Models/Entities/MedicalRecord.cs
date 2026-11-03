namespace Hms.Web.Models.Entities;

public class MedicalRecord
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public Appointment? Appointment { get; set; }

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }

    public Doctor? Doctor { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? BloodPressure { get; set; }

    public string? Temperature { get; set; }

    public string? Pulse { get; set; }

    public string? Weight { get; set; }

    public string? TreatmentPlan { get; set; }

    public string? Observations { get; set; }

    public string? FollowUpRecommendations { get; set; }

    public string Status { get; set; } = EntityStatus.RecordDraft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
