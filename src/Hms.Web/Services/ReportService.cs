using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Report;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public record DashboardCounts(int Patients, int Doctors, int Staff, int Departments);

/// <summary>One point of a chart: a label and its value.</summary>
public record ChartPoint(string Label, decimal Value);

/// <summary>A patient registration shown in the recent-activity list.</summary>
public record RecentPatient(int Id, string PatientCode, string FullName, string Phone, DateTime RegisteredAt);

/// <summary>An appointment shown in the recent-activity list.</summary>
public record RecentAppointment(
    int Id,
    string PatientName,
    string PatientCode,
    string DoctorName,
    DateTime AppointmentDate,
    TimeSpan StartTime,
    string Status);

/// <summary>A department with the number of doctors working in it.</summary>
public record DepartmentDoctorCount(int Id, string Name, string Status, int DoctorCount);

/// <summary>Every figure the Admin dashboard shows, read from the database.</summary>
public record AdminDashboardData(
    int TotalPatients,
    int TotalDoctors,
    int TotalStaff,
    int TotalDepartments,
    int TodaysAppointments,
    int PendingAppointments,
    int CompletedConsultations,
    decimal TotalRevenue,
    decimal OutstandingBalance,
    IReadOnlyList<ChartPoint> RevenueByMonth,
    IReadOnlyList<ChartPoint> AppointmentTrend,
    IReadOnlyList<RecentPatient> RecentPatients,
    IReadOnlyList<RecentAppointment> RecentAppointments,
    IReadOnlyList<DepartmentDoctorCount> Departments);

public interface IReportService
{
    Task<DashboardCounts> GetDashboardCountsAsync();

    Task<int> CountAppointmentsAsync(DateTime from, DateTime to);

    Task<decimal> GetRevenueAsync(DateTime from, DateTime to);

    Task<AdminDashboardData> GetAdminDashboardAsync(DateTime today);

    Task<ReportsIndexViewModel> GetReportsAsync(DateTime? from, DateTime? to);
    Task<PatientReportViewModel> GetPatientReportAsync(ReportPeriod period);
    Task<AppointmentReportViewModel> GetAppointmentReportAsync(ReportPeriod period);
    Task<DepartmentReportViewModel> GetDepartmentReportAsync(ReportPeriod period);
    Task<DoctorReportViewModel> GetDoctorReportAsync(ReportPeriod period);
    Task<LaboratoryReportViewModel> GetLaboratoryReportAsync(ReportPeriod period);
    Task<PharmacyReportViewModel> GetPharmacyReportAsync();
    Task<RevenueReportViewModel> GetRevenueReportAsync(ReportPeriod period);
}

public class ReportService : IReportService
{
    private readonly HmsDbContext _db;

    public ReportService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardCounts> GetDashboardCountsAsync()
    {
        return new DashboardCounts(
            await _db.Patients.CountAsync(),
            await _db.Doctors.CountAsync(),
            await _db.Staff.CountAsync(),
            await _db.Departments.CountAsync());
    }

    public Task<int> CountAppointmentsAsync(DateTime from, DateTime to)
    {
        return _db.Appointments.CountAsync(a => a.AppointmentDate >= from.Date && a.AppointmentDate < to.Date.AddDays(1));
    }

    public async Task<decimal> GetRevenueAsync(DateTime from, DateTime to)
    {
        return await _db.Payments
            .Where(p => p.Status == Models.Entities.EntityStatus.PaymentRecorded
                        && p.PaidAt >= from.Date
                        && p.PaidAt < to.Date.AddDays(1))
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
    }

    public async Task<AdminDashboardData> GetAdminDashboardAsync(DateTime today)
    {
        var day = today.Date;

        var counts = await GetDashboardCountsAsync();

        var todaysAppointments = await _db.Appointments
            .CountAsync(a => a.AppointmentDate >= day && a.AppointmentDate < day.AddDays(1));

        var pendingAppointments = await _db.Appointments
            .CountAsync(a => a.Status == EntityStatus.AppointmentPending);

        var completedConsultations = await _db.MedicalRecords
            .CountAsync(r => r.Status == EntityStatus.RecordFinal);

        // SQLite cannot sum decimals on the server, so the money totals are summed in memory.
        var recordedPayments = await _db.Payments
            .Where(p => p.Status == EntityStatus.PaymentRecorded)
            .Select(p => p.Amount)
            .ToListAsync();
        var totalRevenue = recordedPayments.Sum();

        var openInvoices = await _db.Invoices
            .Where(i => i.Status != EntityStatus.InvoiceCancelled)
            .Select(i => new { i.Total, i.PaidAmount })
            .ToListAsync();
        var outstandingBalance = openInvoices.Sum(i => i.Total - i.PaidAmount);

        // Revenue of the last 6 months, oldest first, one point per calendar month.
        var firstMonth = new DateTime(day.Year, day.Month, 1).AddMonths(-5);
        var payments = await _db.Payments
            .Where(p => p.Status == EntityStatus.PaymentRecorded && p.PaidAt >= firstMonth)
            .Select(p => new { p.PaidAt, p.Amount })
            .ToListAsync();

        var revenueByMonth = new List<ChartPoint>();
        for (var i = 0; i < 6; i++)
        {
            var month = firstMonth.AddMonths(i);
            var sum = payments
                .Where(p => p.PaidAt.Year == month.Year && p.PaidAt.Month == month.Month)
                .Sum(p => p.Amount);
            revenueByMonth.Add(new ChartPoint(month.ToString("MMM yyyy"), sum));
        }

        // Appointment count of the last 7 days, oldest first, one point per day.
        var firstDay = day.AddDays(-6);
        var appointments = await _db.Appointments
            .Where(a => a.AppointmentDate >= firstDay && a.AppointmentDate < day.AddDays(1))
            .Select(a => a.AppointmentDate)
            .ToListAsync();

        var appointmentTrend = new List<ChartPoint>();
        for (var i = 0; i < 7; i++)
        {
            var date = firstDay.AddDays(i);
            var count = appointments.Count(a => a.Date == date);
            appointmentTrend.Add(new ChartPoint(date.ToString("ddd dd MMM"), count));
        }

        var recentPatients = await _db.Patients
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(5)
            .Select(p => new RecentPatient(p.Id, p.PatientCode, p.FullName, p.Phone, p.CreatedAt))
            .ToListAsync();

        // SQLite cannot order by a TimeSpan. Fetch appointments from only the five newest
        // appointment dates, then apply the complete date/time/id ordering in memory.
        var recentAppointmentDates = await _db.Appointments
            .Select(a => a.AppointmentDate)
            .Distinct()
            .OrderByDescending(date => date)
            .Take(5)
            .ToListAsync();

        var oldestRecentAppointmentDate = recentAppointmentDates.Count == 0
            ? (DateTime?)null
            : recentAppointmentDates.Min();

        var recentAppointmentRows = await _db.Appointments
            .Where(a => oldestRecentAppointmentDate == null
                || a.AppointmentDate >= oldestRecentAppointmentDate.Value)
            .Select(a => new RecentAppointment(
                a.Id,
                a.Patient != null ? a.Patient.FullName : string.Empty,
                a.Patient != null ? a.Patient.PatientCode : string.Empty,
                a.Doctor != null ? a.Doctor.FullName : string.Empty,
                a.AppointmentDate,
                a.StartTime,
                a.Status))
            .ToListAsync();

        var recentAppointments = recentAppointmentRows
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ThenByDescending(a => a.Id)
            .Take(5)
            .ToList();

        var departments = await _db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDoctorCount(d.Id, d.Name, d.Status, d.Doctors.Count))
            .ToListAsync();

        return new AdminDashboardData(
            counts.Patients,
            counts.Doctors,
            counts.Staff,
            counts.Departments,
            todaysAppointments,
            pendingAppointments,
            completedConsultations,
            totalRevenue,
            outstandingBalance,
            revenueByMonth,
            appointmentTrend,
            recentPatients,
            recentAppointments,
            departments);
    }

    public async Task<ReportsIndexViewModel> GetReportsAsync(DateTime? from, DateTime? to)
    {
        var period = ReportPeriod.Create(from, to);
        var patients = await GetPatientReportAsync(period);
        var appointments = await GetAppointmentReportAsync(period);
        var departments = await GetDepartmentReportAsync(period);
        var doctors = await GetDoctorReportAsync(period);
        var laboratory = await GetLaboratoryReportAsync(period);
        var pharmacy = await GetPharmacyReportAsync();
        var revenue = await GetRevenueReportAsync(period);
        return new(period, patients, appointments, departments, doctors, laboratory, pharmacy, revenue);
    }

    public async Task<PatientReportViewModel> GetPatientReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var rows = await _db.Patients.AsNoTracking()
            .Where(p => p.CreatedAt >= period.StartDate && p.CreatedAt < endExclusive)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PatientRegistrationRow(p.PatientCode, p.FullName, p.CreatedAt))
            .ToListAsync();
        return new(period, rows);
    }

    public async Task<AppointmentReportViewModel> GetAppointmentReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var rows = await _db.Appointments.AsNoTracking()
            .Where(a => a.AppointmentDate >= period.StartDate && a.AppointmentDate < endExclusive)
            .Select(a => new AppointmentReportRow(
                a.Patient != null ? a.Patient.FullName : string.Empty,
                a.Doctor != null ? a.Doctor.FullName : string.Empty,
                a.AppointmentDate,
                a.Status))
            .ToListAsync();
        rows = rows.OrderByDescending(a => a.Date).ThenBy(a => a.PatientName).ToList();
        var statuses = rows.GroupBy(a => a.Status).OrderBy(g => g.Key).Select(g => new StatusCount(g.Key, g.Count())).ToList();
        return new(period, statuses, rows);
    }

    public async Task<DepartmentReportViewModel> GetDepartmentReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var rows = await _db.Departments.AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new DepartmentReportRow(
                d.Name,
                d.Doctors.Count,
                d.Doctors.SelectMany(doctor => doctor.Appointments)
                    .Count(a => a.AppointmentDate >= period.StartDate && a.AppointmentDate < endExclusive)))
            .ToListAsync();
        return new(period, rows);
    }

    public async Task<DoctorReportViewModel> GetDoctorReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var doctors = await _db.Doctors.AsNoTracking().OrderBy(d => d.FullName)
            .Select(d => new
            {
                d.Id,
                d.FullName,
                Completed = _db.MedicalRecords.Count(r => r.DoctorId == d.Id && r.Status == EntityStatus.RecordFinal
                    && r.UpdatedAt >= period.StartDate && r.UpdatedAt < endExclusive)
            }).ToListAsync();
        var invoiceRows = await _db.Invoices.AsNoTracking()
            .Where(i => i.CreatedAt >= period.StartDate && i.CreatedAt < endExclusive && i.Status != EntityStatus.InvoiceCancelled && i.AppointmentId != null)
            .Select(i => new { DoctorId = i.Appointment!.DoctorId, i.PaidAmount })
            .ToListAsync();
        var revenueByDoctor = invoiceRows.GroupBy(i => i.DoctorId).ToDictionary(g => g.Key, g => g.Sum(i => i.PaidAmount));
        return new(period, doctors.Select(d => new DoctorPerformanceRow(d.FullName, d.Completed, revenueByDoctor.GetValueOrDefault(d.Id))).ToList());
    }

    public async Task<LaboratoryReportViewModel> GetLaboratoryReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var statusValues = await _db.LabRequests.AsNoTracking()
            .Where(r => r.CreatedAt >= period.StartDate && r.CreatedAt < endExclusive)
            .Select(r => r.Status)
            .ToListAsync();
        var statuses = statusValues.GroupBy(status => status)
            .OrderBy(group => group.Key)
            .Select(group => new StatusCount(group.Key, group.Count()))
            .ToList();
        return new(period, statuses, statuses.Where(s => s.Status == EntityStatus.LabCompleted).Sum(s => s.Count));
    }

    public async Task<PharmacyReportViewModel> GetPharmacyReportAsync()
    {
        var today = DateTime.UtcNow.Date;
        var medicines = await _db.Medicines.AsNoTracking().Include(m => m.Category).Include(m => m.Batches).OrderBy(m => m.Name).ToListAsync();
        var rows = medicines.Select(m =>
        {
            var expiry = m.Batches.Count == 0 ? (DateTime?)null : m.Batches.Min(b => b.ExpiryDate);
            var status = expiry?.Date < today ? EntityStatus.MedicineExpired
                : m.StockQuantity == 0 ? EntityStatus.MedicineOutOfStock
                : m.StockQuantity <= m.ReorderLevel ? EntityStatus.MedicineLowStock
                : EntityStatus.MedicineInStock;
            return new PharmacyStockRow(m.Name, m.Category?.Name ?? "—", m.StockQuantity, status, expiry);
        }).ToList();
        return new(rows, rows.Count(m => m.Status == EntityStatus.MedicineLowStock), rows.Count(m => m.Status == EntityStatus.MedicineOutOfStock));
    }

    public async Task<RevenueReportViewModel> GetRevenueReportAsync(ReportPeriod period)
    {
        var endExclusive = period.EndDate.AddDays(1);
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => (p.PaidAt >= period.StartDate && p.PaidAt < endExclusive)
                || (p.Status == EntityStatus.PaymentRefunded
                    && p.UpdatedAt >= period.StartDate
                    && p.UpdatedAt < endExclusive))
            .Select(p => new { p.PaidAt, p.UpdatedAt, p.Amount, p.Status })
            .ToListAsync();
        var recorded = payments
            .Where(p => p.PaidAt >= period.StartDate && p.PaidAt < endExclusive)
            .Sum(p => p.Amount);
        var refunded = payments
            .Where(p => p.Status == EntityStatus.PaymentRefunded
                && p.UpdatedAt >= period.StartDate
                && p.UpdatedAt < endExclusive)
            .Sum(p => p.Amount);
        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.CreatedAt >= period.StartDate && i.CreatedAt < endExclusive && i.Status != EntityStatus.InvoiceCancelled)
            .Select(i => new { i.Total, i.PaidAmount }).ToListAsync();
        var daily = Enumerable.Range(0, (period.EndDate - period.StartDate).Days + 1)
            .Select(offset => period.StartDate.AddDays(offset))
            .Select(day => new RevenueDay(day,
                payments.Where(p => p.PaidAt.Date == day).Sum(p => p.Amount)
                - payments.Where(p => p.Status == EntityStatus.PaymentRefunded && p.UpdatedAt.Date == day).Sum(p => p.Amount)))
            .ToList();
        return new(period, recorded, refunded, recorded - refunded, invoices.Sum(i => i.Total - i.PaidAmount), daily);
    }
}
