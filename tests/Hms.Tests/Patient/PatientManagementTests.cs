using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Patient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hms.Tests.PatientManagement;

/// <summary>
/// Patient management: the P-00001 sequence, registration with status Active, the required-field
/// rule, the paged list with search, sort and status filter, the profile with appointment history,
/// the contact edit and the deactivate that keeps history. Each test migrates its own throwaway
/// SQLite file and asserts only on the rows it writes.
/// </summary>
public class PatientManagementTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly HmsDbContext _db;
    private readonly PatientService _service;

    public PatientManagementTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-patient-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new HmsDbContext(options);
        _db.Database.Migrate();
        _service = new PatientService(_db);
    }

    [Fact]
    public async Task Register_assigns_the_next_patient_code_and_status_active()
    {
        var first = await _service.RegisterAsync(NewPatient("Asha Rao", "9000000001"));
        var second = await _service.RegisterAsync(NewPatient("Bala Iyer", "9000000002"));

        Assert.Equal("P-00001", first.PatientCode);
        Assert.Equal("P-00002", second.PatientCode);
        Assert.Equal(EntityStatus.Active, first.Status);
        Assert.Equal(EntityStatus.Active, second.Status);

        var stored = await _db.Patients.SingleAsync(p => p.Id == second.Id);
        Assert.Equal("P-00002", stored.PatientCode);
        Assert.Equal(EntityStatus.Active, stored.Status);
    }

    [Fact]
    public async Task Register_continues_the_sequence_after_the_highest_existing_code()
    {
        _db.Patients.Add(new Hms.Web.Models.Entities.Patient
        {
            PatientCode = "P-00007",
            FullName = "Existing Patient",
            Phone = "9000000009",
            Status = EntityStatus.Active
        });
        await _db.SaveChangesAsync();

        var created = await _service.RegisterAsync(NewPatient("New Patient", "9000000010"));

        Assert.Equal("P-00008", created.PatientCode);
    }

    [Fact]
    public async Task Register_rejects_a_patient_without_a_full_name_or_phone()
    {
        var noName = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(NewPatient("   ", "9000000003")));
        Assert.Equal("Full name and phone number are required.", noName.Message);

        var noPhone = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(NewPatient("No Phone", "  ")));
        Assert.Equal("Full name and phone number are required.", noPhone.Message);
    }

    [Fact]
    public async Task List_pages_ten_patients_and_searches_by_name_code_or_phone()
    {
        for (var i = 1; i <= 12; i++)
        {
            var patient = NewPatient($"List Patient {i:D2}", $"91100000{i:D2}");
            patient.Email = i == 5 ? "findme@example.test" : null;
            await _service.RegisterAsync(patient);
        }

        var pageOne = await _service.GetPageAsync(null, null, PatientSort.Newest, 1, PatientListViewModel.PageSize);
        var pageTwo = await _service.GetPageAsync(null, null, PatientSort.Newest, 2, PatientListViewModel.PageSize);

        Assert.Equal(12, pageOne.TotalCount);
        Assert.Equal(10, pageOne.Patients.Count);
        Assert.Equal(2, pageTwo.Patients.Count);
        Assert.Equal(2, pageOne.TotalPages);

        var byName = await _service.GetPageAsync("List Patient 07", null, null, 1, PatientListViewModel.PageSize);
        Assert.Single(byName.Patients);
        Assert.Equal("List Patient 07", byName.Patients[0].FullName);

        var byCode = await _service.GetPageAsync("P-00003", null, null, 1, PatientListViewModel.PageSize);
        Assert.Single(byCode.Patients);
        Assert.Equal("P-00003", byCode.Patients[0].PatientCode);

        var byPhone = await _service.GetPageAsync("9110000009", null, null, 1, PatientListViewModel.PageSize);
        Assert.Single(byPhone.Patients);
        Assert.Equal("9110000009", byPhone.Patients[0].Phone);
    }

    [Fact]
    public async Task List_filters_by_status_and_sorts_by_name()
    {
        var active = await _service.RegisterAsync(NewPatient("Zara Active", "9220000001"));
        var inactive = await _service.RegisterAsync(NewPatient("Aaron Inactive", "9220000002"));
        await _service.DeactivateAsync(inactive.Id);

        var inactiveOnly = await _service.GetPageAsync(null, EntityStatus.Inactive, null, 1, PatientListViewModel.PageSize);
        Assert.Contains(inactiveOnly.Patients, p => p.Id == inactive.Id);
        Assert.DoesNotContain(inactiveOnly.Patients, p => p.Id == active.Id);

        var byName = await _service.GetPageAsync(null, null, PatientSort.NameAsc, 1, PatientListViewModel.PageSize);
        var names = byName.Patients.Select(p => p.FullName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Profile_lists_appointments_newest_first_with_doctor_date_time_and_status()
    {
        var patient = await _service.RegisterAsync(NewPatient("History Patient", "9330000001"));
        var doctor = await AddDoctorAsync("Dr History");

        _db.Appointments.AddRange(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDate = new DateTime(2026, 1, 10),
                StartTime = new TimeSpan(9, 0, 0),
                Status = EntityStatus.AppointmentCompleted
            },
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDate = new DateTime(2026, 3, 10),
                StartTime = new TimeSpan(11, 30, 0),
                Status = EntityStatus.AppointmentPending
            });
        await _db.SaveChangesAsync();

        var profile = await _service.GetProfileAsync(patient.Id);

        Assert.NotNull(profile);
        Assert.Equal(patient.Id, profile!.Patient.Id);
        Assert.Equal(2, profile.Appointments.Count);
        Assert.Equal(new DateTime(2026, 3, 10), profile.Appointments[0].AppointmentDate);
        Assert.Equal(new DateTime(2026, 1, 10), profile.Appointments[1].AppointmentDate);
        Assert.Equal("Dr History", profile.Appointments[0].Doctor?.FullName);
        Assert.Equal(new TimeSpan(11, 30, 0), profile.Appointments[0].StartTime);
        Assert.Equal(EntityStatus.AppointmentPending, profile.Appointments[0].Status);
    }

    [Fact]
    public async Task Update_contact_saves_the_contact_and_emergency_details()
    {
        var patient = await _service.RegisterAsync(NewPatient("Edit Patient", "9440000001"));

        var updated = await _service.UpdateContactAsync(new PatientEditViewModel
        {
            Id = patient.Id,
            Phone = "9440000099",
            Email = "edited@example.test",
            Address = "12 New Street",
            EmergencyContactName = "Kin Person",
            EmergencyContactPhone = "9440000088"
        });

        Assert.NotNull(updated);
        var stored = await _db.Patients.AsNoTracking().SingleAsync(p => p.Id == patient.Id);
        Assert.Equal("9440000099", stored.Phone);
        Assert.Equal("edited@example.test", stored.Email);
        Assert.Equal("12 New Street", stored.Address);
        Assert.Equal("Kin Person", stored.EmergencyContactName);
        Assert.Equal("9440000088", stored.EmergencyContactPhone);
    }

    [Fact]
    public async Task Deactivate_sets_inactive_and_keeps_appointments_records_and_invoices()
    {
        var patient = await _service.RegisterAsync(NewPatient("Deactivate Patient", "9550000001"));
        var doctor = await AddDoctorAsync("Dr Keep");

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = new DateTime(2026, 2, 1),
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentCompleted
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _db.MedicalRecords.Add(new MedicalRecord
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Check-up",
            Status = EntityStatus.RecordFinal
        });
        _db.Invoices.Add(new Invoice
        {
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            PatientId = patient.Id,
            Status = EntityStatus.InvoiceUnpaid
        });
        await _db.SaveChangesAsync();

        var deactivated = await _service.DeactivateAsync(patient.Id);

        Assert.True(deactivated);
        var stored = await _db.Patients.AsNoTracking().SingleAsync(p => p.Id == patient.Id);
        Assert.Equal(EntityStatus.Inactive, stored.Status);
        Assert.Equal(1, await _db.Appointments.CountAsync(a => a.PatientId == patient.Id));
        Assert.Equal(1, await _db.MedicalRecords.CountAsync(r => r.PatientId == patient.Id));
        Assert.Equal(1, await _db.Invoices.CountAsync(i => i.PatientId == patient.Id));
    }

    private static Hms.Web.Models.Entities.Patient NewPatient(string fullName, string phone) => new()
    {
        FullName = fullName,
        Phone = phone,
        Gender = "Female",
        DateOfBirth = new DateTime(1990, 5, 1)
    };

    private async Task<Doctor> AddDoctorAsync(string fullName)
    {
        var department = new Department { Name = $"Dept {Guid.NewGuid():N}", Status = EntityStatus.Active };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@example.test",
            Phone = "9000000000",
            Specialization = "General",
            DepartmentId = department.Id,
            Status = EntityStatus.Active
        };
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return doctor;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
