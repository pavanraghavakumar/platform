using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hms.Tests.Clinical;

public class PrescriptionTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public PrescriptionTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-prescription-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Creating_with_a_medicine_saves_an_active_linked_prescription_line()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var created = await service.CreateAsync(new Prescription
        {
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            MedicalRecordId = seed.MedicalRecordId,
            Notes = "Review after the course",
            Items = new List<PrescriptionItem>
            {
                new()
                {
                    MedicineId = seed.MedicineId,
                    Dosage = "500 mg",
                    Frequency = "Twice daily",
                    DurationDays = 7,
                    Instructions = "Take after food"
                }
            }
        });

        var saved = await db.Prescriptions.Include(p => p.Items).SingleAsync(p => p.Id == created.Id);
        Assert.Equal(EntityStatus.PrescriptionActive, saved.Status);
        Assert.Equal(seed.PatientId, saved.PatientId);
        Assert.Equal(seed.DoctorId, saved.DoctorId);
        Assert.Equal(seed.MedicalRecordId, saved.MedicalRecordId);

        var line = Assert.Single(saved.Items);
        Assert.Equal(seed.MedicineId, line.MedicineId);
        Assert.Equal("500 mg", line.Dosage);
        Assert.Equal("Twice daily", line.Frequency);
        Assert.Equal(7, line.DurationDays);
        Assert.Equal("Take after food", line.Instructions);
    }

    [Fact]
    public async Task Creating_without_a_medicine_shows_the_required_message_and_saves_nothing()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new Prescription
        {
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            MedicalRecordId = seed.MedicalRecordId
        }));

        Assert.Equal("Add at least one medicine.", exception.Message);
        Assert.False(await db.Prescriptions.AnyAsync(p => p.MedicalRecordId == seed.MedicalRecordId));
    }

    [Fact]
    public async Task History_is_patient_scoped_and_newest_first_and_print_data_has_each_medicine()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var now = DateTime.UtcNow;

        var older = BuildPrescription(seed, now.AddDays(-2), "Older dose");
        var newer = BuildPrescription(seed, now.AddDays(-1), "Newer dose");
        db.Prescriptions.AddRange(older, newer);
        await db.SaveChangesAsync();

        var history = await service.GetHistoryAsync(seed.PatientId);
        Assert.Equal(new[] { newer.Id, older.Id }, history.Select(p => p.Id));
        Assert.All(history, p => Assert.Equal(seed.PatientId, p.PatientId));
        Assert.All(history, p => Assert.Single(p.Items));

        var printable = await service.GetPrintableAsync(newer.Id);
        Assert.NotNull(printable);
        Assert.Equal("Test Patient", printable!.Patient?.FullName);
        Assert.Equal("P-90001", printable.Patient?.PatientCode);
        Assert.Equal("Dr. Prescriber", printable.Doctor?.FullName);
        Assert.Equal("Paracetamol", Assert.Single(printable.Items).Medicine?.Name);
    }

    [Fact]
    public void Printable_medicine_table_has_phone_safe_labeled_rows()
    {
        var repositoryRoot = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "Hms.Web", "Views", "Prescription", "Print.cshtml"));
        var stylesheet = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "Hms.Web", "wwwroot", "css", "pages", "clinical.css"));

        Assert.Contains("~/css/pages/clinical.css", view);
        Assert.Contains("prescription-medicine-table", view);
        Assert.Contains("data-label=\"Medicine\"", view);
        Assert.Contains("data-label=\"Dosage\"", view);
        Assert.Contains("data-label=\"Frequency\"", view);
        Assert.Contains("data-label=\"Duration\"", view);
        Assert.Contains("data-label=\"Instructions\"", view);
        Assert.Contains(".prescription-medicine-table td::before", stylesheet);
        Assert.Contains("content: attr(data-label)", stylesheet);
    }

    [Theory]
    [InlineData(EntityStatus.PrescriptionCompleted)]
    [InlineData(EntityStatus.PrescriptionCancelled)]
    public async Task Setting_a_terminal_status_saves_old_new_user_and_time_in_the_audit(string newStatus)
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var prescription = BuildPrescription(seed, DateTime.UtcNow, "One tablet");
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();

        Assert.True(await service.SetStatusAsync(prescription.Id, newStatus, seed.DoctorUserName));

        var saved = await db.Prescriptions.SingleAsync(p => p.Id == prescription.Id);
        Assert.Equal(newStatus, saved.Status);

        var audit = await db.AuditEntries.SingleAsync(a =>
            a.RecordType == "Prescription" && a.RecordId == prescription.Id && a.FieldName == "Status");
        Assert.Equal(EntityStatus.PrescriptionActive, audit.OldValue);
        Assert.Equal(newStatus, audit.NewValue);
        Assert.Equal(seed.DoctorUserName, audit.UserName);
        Assert.True(audit.ChangedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Setting_an_unsupported_status_is_rejected_without_an_audit_entry()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var prescription = BuildPrescription(seed, DateTime.UtcNow, "One tablet");
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetStatusAsync(prescription.Id, EntityStatus.PrescriptionActive, seed.DoctorUserName));

        Assert.Equal("Status must be Completed or Cancelled.", exception.Message);
        Assert.Equal(EntityStatus.PrescriptionActive,
            (await db.Prescriptions.SingleAsync(p => p.Id == prescription.Id)).Status);
        Assert.False(await db.AuditEntries.AnyAsync(a => a.RecordType == "Prescription" && a.RecordId == prescription.Id));
    }

    private static Prescription BuildPrescription(SeedData seed, DateTime createdAt, string dosage)
    {
        return new Prescription
        {
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            MedicalRecordId = seed.MedicalRecordId,
            Status = EntityStatus.PrescriptionActive,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Items = new List<PrescriptionItem>
            {
                new()
                {
                    MedicineId = seed.MedicineId,
                    Dosage = dosage,
                    Frequency = "Daily",
                    DurationDays = 5,
                    Instructions = "After food",
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                }
            }
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hms.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed record SeedData(
        int PatientId,
        int DoctorId,
        int MedicalRecordId,
        int MedicineId,
        string DoctorUserName);

    private async Task<SeedData> SeedAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();
        var now = DateTime.UtcNow;

        var department = new Department
        {
            Name = $"Prescription Department {Guid.NewGuid():N}",
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = "Dr. Prescriber",
            Email = "prescriber@hms.local",
            Phone = "9000000011",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var patient = new Patient
        {
            PatientCode = "P-90001",
            FullName = "Test Patient",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            Phone = "9000000012",
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var medicine = new Medicine
        {
            Name = "Paracetamol",
            UnitPrice = 10m,
            StockQuantity = 100,
            ReorderLevel = 10,
            Status = EntityStatus.MedicineInStock,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AddRange(doctor, patient, medicine);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = now.Date,
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentInProgress,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var record = new MedicalRecord
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Viral fever",
            Status = EntityStatus.RecordFinal,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();

        return new SeedData(patient.Id, doctor.Id, record.Id, medicine.Id, "prescriber@hms.local");
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
