namespace Hms.Web.ViewModels.Dashboard;

public class ReceptionistDashboardViewModel
{
    public int TodaysAppointments { get; set; }

    public int PendingAppointments { get; set; }

    public int CheckedInAppointments { get; set; }

    public decimal PaymentsRecordedToday { get; set; }

    public IReadOnlyList<ReceptionistAppointmentRow> Today { get; set; } = Array.Empty<ReceptionistAppointmentRow>();

    public IReadOnlyList<ReceptionistPatientChoice> Patients { get; set; } = Array.Empty<ReceptionistPatientChoice>();

    public IReadOnlyList<ReceptionistNotificationRow> RecentNotifications { get; set; } = Array.Empty<ReceptionistNotificationRow>();
}

public class ReceptionistAppointmentRow
{
    public int Id { get; set; }

    public TimeSpan StartTime { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class ReceptionistPatientChoice
{
    public int Id { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int? InvoiceAppointmentId { get; set; }
}

public class ReceptionistNotificationRow
{
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}
