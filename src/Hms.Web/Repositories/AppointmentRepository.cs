using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Repositories;

public interface IAppointmentRepository
{
    Task<IReadOnlyList<Appointment>> GetForDoctorAsync(int doctorId, DateTime from, DateTime to);

    Task<IReadOnlyList<Appointment>> GetForPatientAsync(int patientId);
}

public class AppointmentRepository : IAppointmentRepository
{
    private readonly HmsDbContext _db;

    public AppointmentRepository(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Appointment>> GetForDoctorAsync(int doctorId, DateTime from, DateTime to)
    {
        return await _db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= from.Date && a.AppointmentDate <= to.Date)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Appointment>> GetForPatientAsync(int patientId)
    {
        return await _db.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToListAsync();
    }
}
