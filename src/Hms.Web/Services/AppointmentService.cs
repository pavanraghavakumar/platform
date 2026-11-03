using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public record SlotDto(string Time, bool Available);

public interface IAppointmentService
{
    Task<IReadOnlyList<SlotDto>> GetFreeSlotsAsync(int doctorId, DateTime date);

    Task<Appointment> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes);

    Task<bool> TryTransitionAsync(int appointmentId, string fromStatus, string toStatus);

    /// <summary>
    /// Books an appointment for the signed-in person. A Patient may only book for themselves;
    /// a Receptionist books for any patient. Returns the created appointment.
    /// </summary>
    Task<Appointment> BookForUserAsync(string userId, bool isReceptionist, int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes);

    /// <summary>Confirms a Pending appointment and notifies the patient.</summary>
    Task<bool> ConfirmAsync(int appointmentId);

    /// <summary>Checks in a Confirmed appointment and notifies the doctor.</summary>
    Task<bool> CheckInAsync(int appointmentId);

    /// <summary>Marks a Confirmed appointment as No Show, freeing the slot.</summary>
    Task<bool> MarkNoShowAsync(int appointmentId);

    /// <summary>Starts a consultation on a Checked In appointment.</summary>
    Task<bool> StartAsync(int appointmentId, int doctorId);

    /// <summary>Completes an In Progress appointment.</summary>
    Task<bool> CompleteAsync(int appointmentId, int doctorId);

    /// <summary>
    /// Cancels a Pending or Confirmed appointment, freeing the slot at once and notifying the other party.
    /// A Patient may cancel only their own appointment.
    /// </summary>
    Task<bool> CancelAsync(int appointmentId, string userId, bool isReceptionist);
}

/// <summary>
/// Free slots, booking with the conflict check, the appointment status transitions,
/// the notifications each transition adds and the ownership checks.
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
    private readonly INotificationService _notifications;

    public AppointmentService(HmsDbContext db)
        : this(db, new NotificationService(db))
    {
    }

    public AppointmentService(HmsDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
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
            if (appointmentDate.Add(time) <= now || taken.Contains(time))
            {
                continue;
            }

            slots.Add(new SlotDto(time.ToString(@"hh\:mm"), true));
        }

        return slots;
    }

    public async Task<Appointment> BookAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor is null || doctor.Status != EntityStatus.Active)
        {
            throw new InvalidOperationException("This doctor is not available for booking.");
        }

        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.Status == EntityStatus.Active);
        if (patient is null)
        {
            throw new InvalidOperationException("Choose a patient.");
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
            throw new InvalidOperationException("This time slot is already booked.");
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
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueSlotConflict(exception))
        {
            _db.Entry(appointment).State = EntityState.Detached;
            throw new InvalidOperationException("This time slot is already booked.");
        }

        return appointment;
    }

    public async Task<Appointment> BookForUserAsync(string userId, bool isReceptionist, int patientId, int doctorId, DateTime date, TimeSpan startTime, string? notes)
    {
        var selectedPatient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.Status == EntityStatus.Active);
        if (selectedPatient is null)
        {
            throw new InvalidOperationException("Choose a patient.");
        }

        if (!isReceptionist)
        {
            if (selectedPatient.UserId != userId)
            {
                throw new InvalidOperationException("You can only book appointments for yourself.");
            }
        }

        return await BookAsync(patientId, doctorId, date, startTime, notes);
    }

    public async Task<bool> TryTransitionAsync(int appointmentId, string fromStatus, string toStatus)
    {
        var updated = await _db.Appointments
            .Where(a => a.Id == appointmentId && a.Status == fromStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, toStatus)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        return updated == 1;
    }

    public async Task<bool> ConfirmAsync(int appointmentId)
    {
        var appointment = await LoadAsync(appointmentId);
        if (appointment is null)
        {
            return false;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var transitioned = await ConditionalTransitionAsync(
            appointmentId,
            EntityStatus.AppointmentPending,
            EntityStatus.AppointmentConfirmed);
        if (!transitioned)
        {
            await transaction.RollbackAsync();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(appointment.Patient?.UserId))
        {
            await _notifications.AddAsync(appointment.Patient.UserId, $"Your appointment with {appointment.Doctor?.FullName} on {Describe(appointment)} is confirmed.");
        }

        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> CheckInAsync(int appointmentId)
    {
        var appointment = await LoadAsync(appointmentId);
        if (appointment is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(appointment.Doctor?.UserId))
        {
            return false;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var transitioned = await ConditionalTransitionAsync(
            appointmentId,
            EntityStatus.AppointmentConfirmed,
            EntityStatus.AppointmentCheckedIn);
        if (!transitioned)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _notifications.AddAsync(appointment.Doctor.UserId, $"{appointment.Patient?.FullName} has checked in for the appointment on {Describe(appointment)}.");

        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> MarkNoShowAsync(int appointmentId)
    {
        return await ConditionalTransitionAsync(
            appointmentId,
            EntityStatus.AppointmentConfirmed,
            EntityStatus.AppointmentNoShow);
    }

    public async Task<bool> StartAsync(int appointmentId, int doctorId)
    {
        var updated = await _db.Appointments
            .Where(a => a.Id == appointmentId
                        && a.DoctorId == doctorId
                        && a.Status == EntityStatus.AppointmentCheckedIn)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, EntityStatus.AppointmentInProgress)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        return updated == 1;
    }

    public async Task<bool> CompleteAsync(int appointmentId, int doctorId)
    {
        var updated = await _db.Appointments
            .Where(a => a.Id == appointmentId
                        && a.DoctorId == doctorId
                        && a.Status == EntityStatus.AppointmentInProgress)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, EntityStatus.AppointmentCompleted)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        return updated == 1;
    }

    public async Task<bool> CancelAsync(int appointmentId, string userId, bool isReceptionist)
    {
        var appointment = await LoadAsync(appointmentId);
        if (appointment is null)
        {
            return false;
        }

        var notificationUserId = isReceptionist
            ? appointment.Patient?.UserId
            : appointment.Doctor?.UserId;

        int? owningPatientId = null;
        if (!isReceptionist)
        {
            var ownPatient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
            if (ownPatient is null || ownPatient.Id != appointment.PatientId)
            {
                return false;
            }

            owningPatientId = ownPatient.Id;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var updated = await _db.Appointments
            .Where(a => a.Id == appointmentId
                        && (!owningPatientId.HasValue || a.PatientId == owningPatientId.Value)
                        && (a.Status == EntityStatus.AppointmentPending
                            || a.Status == EntityStatus.AppointmentConfirmed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, EntityStatus.AppointmentCancelled)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        if (isReceptionist && !string.IsNullOrWhiteSpace(notificationUserId))
        {
            await _notifications.AddAsync(notificationUserId, $"Your appointment with {appointment.Doctor?.FullName} on {Describe(appointment)} was cancelled.");
        }
        else if (!isReceptionist)
        {
            // A Patient can only reach this branch through an account, and doctors always have accounts.
            await _notifications.AddAsync(notificationUserId!, $"{appointment.Patient?.FullName} cancelled the appointment on {Describe(appointment)}.");
        }

        await transaction.CommitAsync();
        return true;
    }

    private Task<Appointment?> LoadAsync(int appointmentId)
    {
        return _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
    }

    private async Task<bool> ConditionalTransitionAsync(int appointmentId, string fromStatus, string toStatus)
    {
        var updated = await _db.Appointments
            .Where(a => a.Id == appointmentId && a.Status == fromStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, toStatus)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        return updated == 1;
    }

    private static string Describe(Appointment appointment)
    {
        return $"{appointment.AppointmentDate:dd MMM yyyy} at {appointment.StartTime:hh\\:mm}";
    }

    private static bool IsUniqueSlotConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not SqliteException sqliteException
            || sqliteException.SqliteExtendedErrorCode != 2067)
        {
            return false;
        }

        var message = sqliteException.Message;
        return message.Contains(
                   "IX_Appointments_DoctorId_AppointmentDate_StartTime",
                   StringComparison.OrdinalIgnoreCase)
               || (message.Contains("Appointments.DoctorId", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("Appointments.AppointmentDate", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("Appointments.StartTime", StringComparison.OrdinalIgnoreCase));
    }
}
