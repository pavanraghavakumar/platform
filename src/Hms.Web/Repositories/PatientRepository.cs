using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetWithDetailsAsync(int id);

    Task<IReadOnlyList<Patient>> GetPageAsync(string? term, int page, int pageSize);
}

public class PatientRepository : IPatientRepository
{
    private readonly HmsDbContext _db;

    public PatientRepository(HmsDbContext db)
    {
        _db = db;
    }

    public Task<Patient?> GetWithDetailsAsync(int id)
    {
        return _db.Patients
            .Include(p => p.Appointments)
            .Include(p => p.MedicalRecords)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Patient>> GetPageAsync(string? term, int page, int pageSize)
    {
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(p => p.FullName.Contains(t) || p.PatientCode.Contains(t) || p.Phone.Contains(t));
        }

        return await query
            .OrderBy(p => p.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
