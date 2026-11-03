using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPrescriptionService
{
    Task<Prescription> CreateAsync(Prescription prescription);

    Task<bool> SetStatusAsync(int prescriptionId, string status, string userName);

    Task<IReadOnlyList<Prescription>> GetHistoryAsync(int patientId);

    Task<Prescription?> GetPrintableAsync(int prescriptionId);
}

public class PrescriptionService : IPrescriptionService
{
    private readonly HmsDbContext _db;
    private readonly IAuditService _audit;

    public PrescriptionService(HmsDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Prescription> CreateAsync(Prescription prescription)
    {
        if (prescription.Items.Count == 0)
        {
            throw new InvalidOperationException("Add at least one medicine.");
        }

        if (prescription.MedicalRecordId is null)
        {
            throw new InvalidOperationException("A consultation is required.");
        }

        foreach (var item in prescription.Items)
        {
            if (item.MedicineId <= 0 || string.IsNullOrWhiteSpace(item.Dosage) ||
                string.IsNullOrWhiteSpace(item.Frequency) || item.DurationDays <= 0)
            {
                throw new InvalidOperationException("Complete every medicine line.");
            }

            item.Dosage = item.Dosage.Trim();
            item.Frequency = item.Frequency.Trim();
            item.Instructions = string.IsNullOrWhiteSpace(item.Instructions) ? null : item.Instructions.Trim();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        var consultationMatches = await _db.MedicalRecords.AnyAsync(r =>
            r.Id == prescription.MedicalRecordId &&
            r.PatientId == prescription.PatientId &&
            r.DoctorId == prescription.DoctorId);
        if (!consultationMatches)
        {
            throw new InvalidOperationException("The consultation does not match this patient and Doctor.");
        }

        var medicineIds = prescription.Items.Select(i => i.MedicineId).Distinct().ToList();
        var medicineCount = await _db.Medicines.CountAsync(m => medicineIds.Contains(m.Id));
        if (medicineCount != medicineIds.Count)
        {
            throw new InvalidOperationException("Select a valid medicine.");
        }

        prescription.Status = EntityStatus.PrescriptionActive;
        prescription.Notes = string.IsNullOrWhiteSpace(prescription.Notes) ? null : prescription.Notes.Trim();
        prescription.CreatedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        return prescription;
    }

    public async Task<bool> SetStatusAsync(int prescriptionId, string status, string userName)
    {
        if (status != EntityStatus.PrescriptionCompleted && status != EntityStatus.PrescriptionCancelled)
        {
            throw new InvalidOperationException("Status must be Completed or Cancelled.");
        }

        var prescription = await _db.Prescriptions.FindAsync(prescriptionId);
        if (prescription is null)
        {
            return false;
        }

        var oldStatus = prescription.Status;
        prescription.Status = status;
        prescription.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.RecordAsync("Prescription", prescription.Id, "Status", oldStatus, status, userName);
        return true;
    }

    public async Task<IReadOnlyList<Prescription>> GetHistoryAsync(int patientId)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Prescription?> GetPrintableAsync(int prescriptionId)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items)
                .ThenInclude(i => i.Medicine)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);
    }
}
