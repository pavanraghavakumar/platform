using Hms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public record DashboardCounts(int Patients, int Doctors, int Staff, int Departments);

public interface IReportService
{
    Task<DashboardCounts> GetDashboardCountsAsync();

    Task<int> CountAppointmentsAsync(DateTime from, DateTime to);

    Task<decimal> GetRevenueAsync(DateTime from, DateTime to);
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
}
