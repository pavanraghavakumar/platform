using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPrescriptionService
{
    Task<Prescription> CreateAsync(Prescription prescription);

    Task<bool> SetStatusAsync(int prescriptionId, string status, string userName);
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

        prescription.Status = EntityStatus.PrescriptionActive;
        prescription.CreatedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        return prescription;
    }

    public async Task<bool> SetStatusAsync(int prescriptionId, string status, string userName)
    {
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
}
