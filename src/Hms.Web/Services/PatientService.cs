using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPatientService
{
    Task<string> NextPatientCodeAsync();

    Task<IReadOnlyList<Patient>> SearchAsync(string? term);

    Task<Patient> RegisterAsync(Patient patient);
}

public class PatientService : IPatientService
{
    private readonly HmsDbContext _db;

    public PatientService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextPatientCodeAsync()
    {
        var last = await _db.Patients
            .OrderByDescending(p => p.Id)
            .Select(p => p.PatientCode)
            .FirstOrDefaultAsync();

        var next = 1;
        if (!string.IsNullOrEmpty(last) && last.StartsWith("P-") && int.TryParse(last[2..], out var number))
        {
            next = number + 1;
        }

        return $"P-{next:D5}";
    }

    public async Task<IReadOnlyList<Patient>> SearchAsync(string? term)
    {
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(p =>
                p.FullName.Contains(t) || p.PatientCode.Contains(t) || p.Phone.Contains(t));
        }

        return await query.OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<Patient> RegisterAsync(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FullName) || string.IsNullOrWhiteSpace(patient.Phone))
        {
            throw new InvalidOperationException("Full name and phone number are required.");
        }

        patient.PatientCode = await NextPatientCodeAsync();
        patient.Status = EntityStatus.Active;
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }
}
