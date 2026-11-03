using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IConsultationService
{
    Task<MedicalRecord?> GetByAppointmentAsync(int appointmentId);

    /// <summary>The patient's last <paramref name="take"/> consultations, newest first.</summary>
    Task<IReadOnlyList<MedicalRecord>> GetRecentConsultationsAsync(int patientId, int take = 5);

    /// <summary>True when the patient has at least one appointment with the doctor.</summary>
    Task<bool> DoctorHasAppointmentWithPatientAsync(int doctorId, int patientId);

    Task<MedicalRecord> SaveDraftAsync(MedicalRecord record, string userName);

    Task<bool> FinalizeAsync(int medicalRecordId);
}

/// <summary>
/// Medical record create/update/finalize and the patient history the consultation page shows.
/// Editing a Final record writes an audit entry for each field that changed.
/// </summary>
public class ConsultationService : IConsultationService
{
    public const string RecordType = "MedicalRecord";

    private readonly HmsDbContext _db;
    private readonly IAuditService _audit;

    public ConsultationService(HmsDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public Task<MedicalRecord?> GetByAppointmentAsync(int appointmentId)
    {
        return _db.MedicalRecords.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetRecentConsultationsAsync(int patientId, int take = 5)
    {
        return await _db.MedicalRecords
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public Task<bool> DoctorHasAppointmentWithPatientAsync(int doctorId, int patientId)
    {
        return _db.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patientId);
    }

    public async Task<MedicalRecord> SaveDraftAsync(MedicalRecord record, string userName)
    {
        if (string.IsNullOrWhiteSpace(record.Diagnosis))
        {
            throw new InvalidOperationException("Diagnosis is required.");
        }

        record.UpdatedAt = DateTime.UtcNow;

        if (record.Id == 0)
        {
            record.Status = EntityStatus.RecordDraft;
            record.CreatedAt = DateTime.UtcNow;
            _db.MedicalRecords.Add(record);
            await _db.SaveChangesAsync();
            return record;
        }

        var existing = await _db.MedicalRecords
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(r => r.Id == record.Id)
            ?? throw new InvalidOperationException("Medical record not found.");

        userName = existing.Doctor?.User?.UserName
            ?? existing.Doctor?.Email
            ?? userName;

        var wasFinal = existing.Status == EntityStatus.RecordFinal;

        // A Final record keeps its status when edited; a Draft stays Draft.
        record.Status = existing.Status;

        var changes = new List<(string Field, string? OldValue, string? NewValue)>
        {
            ("Symptoms", existing.Symptoms, record.Symptoms),
            ("Diagnosis", existing.Diagnosis, record.Diagnosis),
            ("BloodPressure", existing.BloodPressure, record.BloodPressure),
            ("Temperature", existing.Temperature, record.Temperature),
            ("Pulse", existing.Pulse, record.Pulse),
            ("Weight", existing.Weight, record.Weight),
            ("TreatmentPlan", existing.TreatmentPlan, record.TreatmentPlan),
            ("Observations", existing.Observations, record.Observations),
            ("FollowUpRecommendations", existing.FollowUpRecommendations, record.FollowUpRecommendations)
        };

        existing.Symptoms = record.Symptoms;
        existing.Diagnosis = record.Diagnosis;
        existing.BloodPressure = record.BloodPressure;
        existing.Temperature = record.Temperature;
        existing.Pulse = record.Pulse;
        existing.Weight = record.Weight;
        existing.TreatmentPlan = record.TreatmentPlan;
        existing.Observations = record.Observations;
        existing.FollowUpRecommendations = record.FollowUpRecommendations;
        existing.UpdatedAt = record.UpdatedAt;

        await _db.SaveChangesAsync();

        if (wasFinal)
        {
            foreach (var (field, oldValue, newValue) in changes)
            {
                await _audit.RecordAsync(RecordType, existing.Id, field, oldValue, newValue, userName);
            }
        }

        return existing;
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
