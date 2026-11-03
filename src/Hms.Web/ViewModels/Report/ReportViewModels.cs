namespace Hms.Web.ViewModels.Report;

public record ReportPeriod(DateTime StartDate, DateTime EndDate)
{
    public static ReportPeriod Create(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.UtcNow).Date;
        var start = (from ?? end.AddDays(-29)).Date;
        return start <= end ? new(start, end) : new(end, start);
    }
}

public record PatientRegistrationRow(string PatientCode, string Name, DateTime RegisteredAt);
public record PatientReportViewModel(ReportPeriod Period, IReadOnlyList<PatientRegistrationRow> Patients)
{
    public int Total => Patients.Count;
}

public record StatusCount(string Status, int Count);
public record AppointmentReportRow(string PatientName, string DoctorName, DateTime Date, string Status);
public record AppointmentReportViewModel(ReportPeriod Period, IReadOnlyList<StatusCount> Statuses, IReadOnlyList<AppointmentReportRow> Appointments);

public record DepartmentReportRow(string DepartmentName, int DoctorCount, int AppointmentCount);
public record DepartmentReportViewModel(ReportPeriod Period, IReadOnlyList<DepartmentReportRow> Departments);

public record DoctorPerformanceRow(string DoctorName, int CompletedConsultations, decimal Revenue);
public record DoctorReportViewModel(ReportPeriod Period, IReadOnlyList<DoctorPerformanceRow> Doctors);

public record LaboratoryReportViewModel(ReportPeriod Period, IReadOnlyList<StatusCount> Statuses, int CompletedResults)
{
    public int TotalRequests => Statuses.Sum(item => item.Count);
}

public record PharmacyStockRow(string MedicineName, string CategoryName, int StockQuantity, string Status, DateTime? EarliestExpiryDate);
public record PharmacyReportViewModel(IReadOnlyList<PharmacyStockRow> Medicines, int LowStockCount, int OutOfStockCount);

public record RevenueDay(DateTime Date, decimal NetRevenue);
public record RevenueReportViewModel(
    ReportPeriod Period,
    decimal TotalPaymentsRecorded,
    decimal TotalRefunded,
    decimal NetRevenue,
    decimal OutstandingBalance,
    IReadOnlyList<RevenueDay> DailyRevenue);

public record ReportsIndexViewModel(
    ReportPeriod Period,
    PatientReportViewModel Patients,
    AppointmentReportViewModel Appointments,
    DepartmentReportViewModel Departments,
    DoctorReportViewModel Doctors,
    LaboratoryReportViewModel Laboratory,
    PharmacyReportViewModel Pharmacy,
    RevenueReportViewModel Revenue);
