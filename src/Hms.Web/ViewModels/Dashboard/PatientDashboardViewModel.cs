using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Dashboard;

public class PatientDashboardViewModel
{
    public PatientAppointmentRow? NextAppointment { get; set; }
    public int UpcomingAppointments { get; set; }
    public int UnpaidInvoices { get; set; }
    public decimal OutstandingBalance { get; set; }
    public IReadOnlyList<PatientNotificationRow> RecentNotifications { get; set; } = Array.Empty<PatientNotificationRow>();
}

public class PatientAppointmentRow
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PatientNotificationRow
{
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class PatientDoctorRow
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
}

public class PatientBookingViewModel
{
    public IReadOnlyList<PatientDoctorRow> Doctors { get; set; } = Array.Empty<PatientDoctorRow>();

    [Range(1, int.MaxValue, ErrorMessage = "Choose a doctor.")]
    public int DoctorId { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Choose a time slot.")]
    public string? Time { get; set; }
}

public class PatientRecordRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
}

public class PatientPrescriptionRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PatientLabReportRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class PatientInvoiceRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Outstanding => Math.Max(0, Total - PaidAmount);
}
