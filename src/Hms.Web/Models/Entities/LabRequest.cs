namespace Hms.Web.Models.Entities;

public class LabRequest
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }

    public Doctor? Doctor { get; set; }

    public int? AppointmentId { get; set; }

    public Appointment? Appointment { get; set; }

    public int LaboratoryTestId { get; set; }

    public LaboratoryTest? LaboratoryTest { get; set; }

    public string Status { get; set; } = EntityStatus.LabRequested;

    public string? ResultValue { get; set; }

    public string? Remarks { get; set; }

    public DateTime? ResultEnteredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
