using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public record SlotDto(string Time, bool Available);

public interface IAppointmentService
{
    Task<IReadOnlyList<SlotDto>> GetFreeSlotsAsync(int doctorId, DateTime date);

    Task<Appointment> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes);

    Task<bool> TryTransitionAsync(int appointmentId, string fromStatus, string toStatus);
}

/// <summary>
/// Free slots, booking with the conflict check, and the appointment status transitions.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private static readonly string[] BlockingStatuses =
    {
        EntityStatus.AppointmentPending,
        EntityStatus.AppointmentConfirmed,
        EntityStatus.AppointmentCheckedIn,
        EntityStatus.AppointmentInProgress
    };

    private readonly HmsDbContext _db;

    public AppointmentService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SlotDto>> GetFreeSlotsAsync(int doctorId, DateTime date)
    {
        var appointmentDate = date.Date;
        var now = DateTime.UtcNow;

        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId && d.Status == EntityStatus.Active))
        {
            return Array.Empty<SlotDto>();
        }

        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == appointmentDate.DayOfWeek);

        if (schedule is null || !schedule.IsWorking || schedule.StartTime is null || schedule.EndTime is null)
        {
            return Array.Empty<SlotDto>();
        }

        var taken = await _db.Appointments
            .Where(a => a.DoctorId == doctorId
                        && a.AppointmentDate.Date == appointmentDate
                        && BlockingStatuses.Contains(a.Status))
            .Select(a => a.StartTime)
            .ToListAsync();

        var slots = new List<SlotDto>();
        for (var time = schedule.StartTime.Value; time < schedule.EndTime.Value; time = time.Add(TimeSpan.FromMinutes(30)))
        {
            if (appointmentDate.Add(time) <= now)
            {
                continue;
            }

            slots.Add(new SlotDto(time.ToString(@"hh\:mm"), !taken.Contains(time)));
        }

        return slots;
    }

    public async Task<Appointment> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId && d.Status == EntityStatus.Active))
        {
            throw new InvalidOperationException("This doctor is not available for booking.");
        }

        var appointmentDate = date.Date;
        var schedule = await _db.DoctorSchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == appointmentDate.DayOfWeek);
        var slotLength = TimeSpan.FromMinutes(30);
        var isScheduledSlot = schedule is { IsWorking: true, StartTime: not null, EndTime: not null }
            && startTime >= schedule.StartTime.Value
            && startTime < schedule.EndTime.Value
            && (startTime - schedule.StartTime.Value).Ticks % slotLength.Ticks == 0;

        if (!isScheduledSlot || appointmentDate.Add(startTime) <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Choose a free time slot.");
        }

        var conflict = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId
            && a.AppointmentDate.Date == appointmentDate
            && a.StartTime == startTime
            && BlockingStatuses.Contains(a.Status));

        if (conflict)
        {
            throw new InvalidOperationException("This time slot is already booked.");
        }

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate,
            StartTime = startTime,
            Status = EntityStatus.AppointmentPending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }

    public async Task<bool> TryTransitionAsync(int appointmentId, string fromStatus, string toStatus)
    {
        var appointment = await _db.Appointments.FindAsync(appointmentId);
        if (appointment is null || appointment.Status != fromStatus)
        {
            return false;
        }

        appointment.Status = toStatus;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
