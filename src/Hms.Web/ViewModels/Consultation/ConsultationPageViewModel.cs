using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Consultation;

/// <summary>
/// The consultation page: the appointment being seen, the patient's details, allergies and
/// medical history, the last 5 consultations with their diagnosis and date, and the form
/// that saves the Draft medical record.
/// </summary>
public class ConsultationPageViewModel
{
    public int AppointmentId { get; set; }

    public string AppointmentStatus { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string? PatientGender { get; set; }

    public DateTime PatientDateOfBirth { get; set; }

    public string? PatientPhone { get; set; }

    public string? PatientEmail { get; set; }

    public string? PatientAddress { get; set; }

    public string? Allergies { get; set; }

    public string? MedicalHistory { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    /// <summary>The last 5 consultations of this patient, newest first.</summary>
    public IReadOnlyList<ConsultationHistoryItem> RecentConsultations { get; set; } = Array.Empty<ConsultationHistoryItem>();

    public ConsultationFormViewModel Form { get; set; } = new();
}

/// <summary>One past consultation shown in the history panel.</summary>
public class ConsultationHistoryItem
{
    public int MedicalRecordId { get; set; }

    public DateTime Date { get; set; }

    public string? Diagnosis { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

/// <summary>The fields of the consultation form that saves a Draft medical record.</summary>
public class ConsultationFormViewModel
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

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

    public bool IsFinal => Status == EntityStatus.RecordFinal;
}
