using Hms.Web.Services;

namespace Hms.Web.ViewModels.Dashboard;

/// <summary>The model of the Admin dashboard at <c>/Dashboard/Admin</c>.</summary>
public class AdminDashboardViewModel
{
    public int TotalPatients { get; set; }

    public int TotalDoctors { get; set; }

    public int TotalStaff { get; set; }

    public int TotalDepartments { get; set; }

    public int TodaysAppointments { get; set; }

    public int PendingAppointments { get; set; }

    public int CompletedConsultations { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal OutstandingBalance { get; set; }

    /// <summary>Revenue of the last 6 months, oldest first.</summary>
    public IReadOnlyList<ChartPoint> RevenueByMonth { get; set; } = Array.Empty<ChartPoint>();

    /// <summary>Appointment count of the last 7 days, oldest first.</summary>
    public IReadOnlyList<ChartPoint> AppointmentTrend { get; set; } = Array.Empty<ChartPoint>();

    /// <summary>The 5 most recent patient registrations, newest first.</summary>
    public IReadOnlyList<RecentPatient> RecentPatients { get; set; } = Array.Empty<RecentPatient>();

    /// <summary>The 5 most recent appointments, newest first.</summary>
    public IReadOnlyList<RecentAppointment> RecentAppointments { get; set; } = Array.Empty<RecentAppointment>();

    /// <summary>Every department with the number of doctors in it.</summary>
    public IReadOnlyList<DepartmentDoctorCount> Departments { get; set; } = Array.Empty<DepartmentDoctorCount>();

    public static AdminDashboardViewModel FromData(AdminDashboardData data) => new()
    {
        TotalPatients = data.TotalPatients,
        TotalDoctors = data.TotalDoctors,
        TotalStaff = data.TotalStaff,
        TotalDepartments = data.TotalDepartments,
        TodaysAppointments = data.TodaysAppointments,
        PendingAppointments = data.PendingAppointments,
        CompletedConsultations = data.CompletedConsultations,
        TotalRevenue = data.TotalRevenue,
        OutstandingBalance = data.OutstandingBalance,
        RevenueByMonth = data.RevenueByMonth,
        AppointmentTrend = data.AppointmentTrend,
        RecentPatients = data.RecentPatients,
        RecentAppointments = data.RecentAppointments,
        Departments = data.Departments
    };
}
