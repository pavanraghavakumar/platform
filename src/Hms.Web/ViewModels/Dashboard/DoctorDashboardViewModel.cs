namespace Hms.Web.ViewModels.Dashboard;

public class DoctorDashboardViewModel
{
    public int TodaysAppointments { get; set; }

    public int CheckedInWaiting { get; set; }

    public int CompletedConsultationsToday { get; set; }

    public int LabResultsCompletedToday { get; set; }

    public IReadOnlyList<DoctorAppointmentRow> Today { get; set; } = Array.Empty<DoctorAppointmentRow>();

    public IReadOnlyList<DoctorAppointmentRow> Upcoming { get; set; } = Array.Empty<DoctorAppointmentRow>();

    public IReadOnlyList<DoctorNotificationRow> RecentNotifications { get; set; } = Array.Empty<DoctorNotificationRow>();
}

public class DoctorAppointmentRow
{
    public int Id { get; set; }

    public DateTime AppointmentDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PatientCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class DoctorNotificationRow
{
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}

public class DoctorPatientRow
{
    public int Id { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateTime LastAppointmentDate { get; set; }

    public string Status { get; set; } = string.Empty;
}
