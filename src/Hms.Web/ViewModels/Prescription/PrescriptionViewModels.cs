using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Prescription;

public class PrescriptionCreateViewModel
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public int MedicalRecordId { get; set; }

    public string? Notes { get; set; }

    public List<PrescriptionMedicineOption> Medicines { get; set; } = new();

    public List<PrescriptionLineViewModel> Items { get; set; } = new();
}

public class PrescriptionLineViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a medicine.")]
    public int MedicineId { get; set; }

    [Required]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    public string Frequency { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "Duration must be at least one day.")]
    public int DurationDays { get; set; }

    public string? Instructions { get; set; }
}

public class PrescriptionMedicineOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PrescriptionHistoryViewModel
{
    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public IReadOnlyList<PrescriptionHistoryRow> Prescriptions { get; set; } = Array.Empty<PrescriptionHistoryRow>();
}

public class PrescriptionHistoryRow
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int MedicineCount { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class PrescriptionPrintViewModel
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public IReadOnlyList<PrescriptionPrintLine> Items { get; set; } = Array.Empty<PrescriptionPrintLine>();
}

public class PrescriptionPrintLine
{
    public string MedicineName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public int DurationDays { get; set; }

    public string? Instructions { get; set; }
}
