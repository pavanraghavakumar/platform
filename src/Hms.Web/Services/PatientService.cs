using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Patient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPatientService
{
    Task<string> NextPatientCodeAsync();

    Task<IReadOnlyList<Patient>> SearchAsync(string? term);

    Task<Patient> RegisterAsync(Patient patient);

    /// <summary>Registers a patient together with the Patient account used for portal access and notifications.</summary>
    Task<Patient> RegisterWithAccountAsync(Patient patient, string password);

    /// <summary>One page of the patient list, filtered by search text and status and ordered by the sort option.</summary>
    Task<PatientListViewModel> GetPageAsync(string? search, string? status, string? sort, int page, int pageSize);

    /// <summary>The patient with its appointments (and their doctors) for the profile page.</summary>
    Task<PatientDetailsViewModel?> GetProfileAsync(int id);

    /// <summary>Saves the contact and emergency details of an existing patient.</summary>
    Task<Patient?> UpdateContactAsync(PatientEditViewModel model);

    /// <summary>Sets the patient Inactive, keeping appointments, medical records and invoices.</summary>
    Task<bool> DeactivateAsync(int id);
}

public class PatientService : IPatientService
{
    private readonly HmsDbContext _db;
    private readonly UserManager<ApplicationUser>? _users;

    public PatientService(HmsDbContext db)
    {
        _db = db;
    }

    public PatientService(HmsDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<string> NextPatientCodeAsync()
    {
        var codes = await _db.Patients
            .Where(p => p.PatientCode.StartsWith("P-"))
            .Select(p => p.PatientCode)
            .ToListAsync();

        var highest = 0;
        foreach (var code in codes)
        {
            if (code.Length > 2 && int.TryParse(code[2..], out var number) && number > highest)
            {
                highest = number;
            }
        }

        return $"P-{highest + 1:D5}";
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

        patient.FullName = patient.FullName.Trim();
        patient.Phone = patient.Phone.Trim();
        patient.PatientCode = await NextPatientCodeAsync();
        patient.Status = EntityStatus.Active;
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    public async Task<Patient> RegisterWithAccountAsync(Patient patient, string password)
    {
        if (_users is null)
        {
            throw new InvalidOperationException("Patient account registration is not available.");
        }

        if (string.IsNullOrWhiteSpace(patient.FullName) || string.IsNullOrWhiteSpace(patient.Phone))
        {
            throw new InvalidOperationException("Full name and phone number are required.");
        }

        var email = patient.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email and sign-in password are required.");
        }

        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = patient.FullName.Trim(),
            PhoneNumber = patient.Phone.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        var roleAdded = await _users.AddToRoleAsync(user, Roles.Patient);
        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", roleAdded.Errors.Select(error => error.Description)));
        }

        patient.FullName = patient.FullName.Trim();
        patient.Phone = patient.Phone.Trim();
        patient.Email = email;
        patient.PatientCode = await NextPatientCodeAsync();
        patient.Status = EntityStatus.Active;
        patient.UserId = user.Id;
        patient.CreatedAt = now;
        patient.UpdatedAt = now;

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return patient;
    }

    public async Task<PatientListViewModel> GetPageAsync(string? search, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term) || p.PatientCode.Contains(term) || p.Phone.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var total = await query.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        var currentPage = Math.Clamp(page, 1, totalPages);

        query = PatientSort.Normalise(sort) switch
        {
            PatientSort.Oldest => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            PatientSort.NameAsc => query.OrderBy(p => p.FullName).ThenBy(p => p.Id),
            PatientSort.NameDesc => query.OrderByDescending(p => p.FullName).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var patients = await query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PatientListViewModel
        {
            Patients = patients,
            Search = search,
            Status = status,
            Sort = PatientSort.Normalise(sort),
            Page = currentPage,
            TotalCount = total
        };
    }

    public async Task<PatientDetailsViewModel?> GetProfileAsync(int id)
    {
        var patient = await _db.Patients
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
        {
            return null;
        }

        return new PatientDetailsViewModel
        {
            Patient = patient,
            Appointments = patient.Appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToList(),
            MedicalRecordCount = await _db.MedicalRecords.CountAsync(r => r.PatientId == id),
            InvoiceCount = await _db.Invoices.CountAsync(i => i.PatientId == id)
        };
    }

    public async Task<Patient?> UpdateContactAsync(PatientEditViewModel model)
    {
        var patient = await _db.Patients.FindAsync(model.Id);
        if (patient is null)
        {
            return null;
        }

        patient.Phone = model.Phone?.Trim() ?? string.Empty;
        patient.Email = model.Email?.Trim();
        patient.Address = model.Address?.Trim();
        patient.EmergencyContactName = model.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = model.EmergencyContactPhone?.Trim();
        patient.MedicalHistory = model.MedicalHistory;
        patient.Allergies = model.Allergies;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return patient;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null)
        {
            return false;
        }

        // Soft status only: appointments, medical records and invoices are deliberately kept.
        patient.Status = EntityStatus.Inactive;
        patient.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
