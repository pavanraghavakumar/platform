using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface ILabService
{
    Task<IReadOnlyList<LabRequest>> CreateRequestsAsync(int patientId, int doctorId, int? appointmentId, IEnumerable<int> testIds);

    Task<bool> StartAsync(int labRequestId);

    Task<bool> CompleteAsync(int labRequestId, string resultValue, string? remarks);

    /// <summary>The lab request list, newest first, optionally filtered by status (Requirement 8.3).</summary>
    Task<IReadOnlyList<LabRequest>> GetRequestsAsync(string? status);

    /// <summary>One lab request with its patient, doctor, test and appointment, or <c>null</c>.</summary>
    Task<LabRequest?> GetRequestAsync(int labRequestId);

    /// <summary>
    /// Whether the signed-in Doctor may open this request: only the Doctor who requested it
    /// (Requirement 8.7). A request with no requesting Doctor is not accessible to any Doctor.
    /// </summary>
    Task<bool> IsRequestedByDoctorAsync(int labRequestId, string? userId);

    /// <summary>Whether the signed-in Patient owns this request (Requirement 8.8).</summary>
    Task<bool> IsOwnedByPatientAsync(int labRequestId, string? userId);
}

public class LabService : ILabService
{
    private readonly HmsDbContext _db;
    private readonly INotificationService _notifications;

    public LabService(HmsDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<LabRequest>> CreateRequestsAsync(int patientId, int doctorId, int? appointmentId, IEnumerable<int> testIds)
    {
        var created = new List<LabRequest>();
        foreach (var testId in testIds.Distinct())
        {
            var request = new LabRequest
            {
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentId = appointmentId,
                LaboratoryTestId = testId,
                Status = EntityStatus.LabRequested,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.LabRequests.Add(request);
            created.Add(request);
        }

        await _db.SaveChangesAsync();
        return created;
    }

    public async Task<bool> StartAsync(int labRequestId)
    {
        var request = await _db.LabRequests.FindAsync(labRequestId);
        if (request is null || request.Status != EntityStatus.LabRequested)
        {
            return false;
        }

        request.Status = EntityStatus.LabInProgress;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAsync(int labRequestId, string resultValue, string? remarks)
    {
        var request = await _db.LabRequests.FindAsync(labRequestId);
        if (request is null || request.Status != EntityStatus.LabInProgress)
        {
            return false;
        }

        request.Status = EntityStatus.LabCompleted;
        request.ResultValue = resultValue;
        request.Remarks = remarks;
        request.ResultEnteredAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var doctor = await _db.Doctors.FindAsync(request.DoctorId);
        if (doctor?.UserId is not null)
        {
            await _notifications.AddAsync(doctor.UserId, "A laboratory result is ready for one of your patients.");
        }

        return true;
    }

    public async Task<IReadOnlyList<LabRequest>> GetRequestsAsync(string? status)
    {
        var query = _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.LaboratoryTest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public Task<LabRequest?> GetRequestAsync(int labRequestId)
    {
        return _db.LabRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.LaboratoryTest)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == labRequestId);
    }

    public async Task<bool> IsRequestedByDoctorAsync(int labRequestId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return await _db.LabRequests
            .AnyAsync(r => r.Id == labRequestId && r.Doctor != null && r.Doctor.UserId == userId);
    }

    public async Task<bool> IsOwnedByPatientAsync(int labRequestId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return await _db.LabRequests
            .AnyAsync(r => r.Id == labRequestId && r.Patient != null && r.Patient.UserId == userId);
    }
}
