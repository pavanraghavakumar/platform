using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.MedicalRecord;

/// <summary>A patient's medical records, newest first, with date, doctor, diagnosis and status.</summary>
public class MedicalRecordListViewModel
{
    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public IReadOnlyList<MedicalRecordRow> Records { get; set; } = Array.Empty<MedicalRecordRow>();
}

/// <summary>One row of the medical record list.</summary>
public class MedicalRecordRow
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string? Diagnosis { get; set; }

    public string Status { get; set; } = string.Empty;
}

/// <summary>One medical record with every field the Doctor recorded.</summary>
public class MedicalRecordDetailsViewModel
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

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
}
