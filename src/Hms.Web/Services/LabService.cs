using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface ILabService
{
    Task<IReadOnlyList<LabRequest>> CreateRequestsAsync(int patientId, int doctorId, int? appointmentId, IEnumerable<int> testIds);

    Task<bool> StartAsync(int labRequestId);

    Task<bool> CompleteAsync(int labRequestId, string resultValue, string? remarks);
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
        foreach (var testId in testIds)
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
}
