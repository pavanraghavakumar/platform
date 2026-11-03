namespace Hms.Web.Models.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }

    public Doctor? Doctor { get; set; }

    public DateTime AppointmentDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public string Status { get; set; } = EntityStatus.AppointmentPending;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public MedicalRecord? MedicalRecord { get; set; }

    public Invoice? Invoice { get; set; }

    public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();
}
