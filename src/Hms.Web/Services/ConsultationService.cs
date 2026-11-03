using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IConsultationService
{
    Task<MedicalRecord?> GetByAppointmentAsync(int appointmentId);

    Task<MedicalRecord> SaveDraftAsync(MedicalRecord record);

    Task<bool> FinalizeAsync(int medicalRecordId);
}

public class ConsultationService : IConsultationService
{
    private readonly HmsDbContext _db;

    public ConsultationService(HmsDbContext db)
    {
        _db = db;
    }

    public Task<MedicalRecord?> GetByAppointmentAsync(int appointmentId)
    {
        return _db.MedicalRecords.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
    }

    public async Task<MedicalRecord> SaveDraftAsync(MedicalRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Diagnosis))
        {
            throw new InvalidOperationException("Diagnosis is required.");
        }

        record.Status = EntityStatus.RecordDraft;
        record.UpdatedAt = DateTime.UtcNow;

        if (record.Id == 0)
        {
            record.CreatedAt = DateTime.UtcNow;
            _db.MedicalRecords.Add(record);
        }
        else
        {
            _db.MedicalRecords.Update(record);
        }

        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> FinalizeAsync(int medicalRecordId)
    {
        var record = await _db.MedicalRecords.FindAsync(medicalRecordId);
        if (record is null)
        {
            return false;
        }

        record.Status = EntityStatus.RecordFinal;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
