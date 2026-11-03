using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Consultation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hms.Tests.Clinical;

/// <summary>
/// Consultation page and medical records: the patient history the consultation page shows,
/// saving a Draft record, the "Diagnosis is required." rule, finalizing to Final, the record
/// list order, the Doctor ownership check, and the audit entry when a Final record is edited.
/// </summary>
public class ConsultationTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public ConsultationTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-consultation-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IConsultationService, ConsultationService>();

        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Recent_consultations_returns_the_last_five_newest_first()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();

        var history = await consultations.GetRecentConsultationsAsync(seed.PatientId, 5);

        Assert.Equal(5, history.Count);
        Assert.Equal("Diagnosis 1", history[0].Diagnosis);
        Assert.Equal("Diagnosis 5", history[4].Diagnosis);
        Assert.DoesNotContain(history, r => r.Diagnosis == "Diagnosis 6");
    }

    [Fact]
    public async Task Saving_a_consultation_creates_a_draft_medical_record()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var record = await consultations.SaveDraftAsync(new MedicalRecord
        {
            AppointmentId = seed.CheckedInAppointmentId,
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            Symptoms = "Chest pain",
            Diagnosis = "Angina",
            BloodPressure = "130/85",
            Temperature = "37.1 C",
            Pulse = "78",
            Weight = "72 kg",
            TreatmentPlan = "Rest and medication",
            Observations = "Stable",
            FollowUpRecommendations = "Review in 1 week"
        }, seed.DoctorEmail);

        var saved = await db.MedicalRecords.SingleAsync(r => r.AppointmentId == seed.CheckedInAppointmentId);
        Assert.Equal(EntityStatus.RecordDraft, saved.Status);
        Assert.Equal("Angina", saved.Diagnosis);
        Assert.Equal("130/85", saved.BloodPressure);
        Assert.Equal("72 kg", saved.Weight);
        Assert.Equal(record.Id, saved.Id);
    }

    [Fact]
    public async Task Saving_without_a_diagnosis_throws_the_required_message_and_saves_nothing()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consultations.SaveDraftAsync(new MedicalRecord
        {
            AppointmentId = seed.CheckedInAppointmentId,
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            Symptoms = "Headache",
            Diagnosis = ""
        }, seed.DoctorEmail));

        Assert.Equal("Diagnosis is required.", ex.Message);
        Assert.False(await db.MedicalRecords.AnyAsync(r => r.AppointmentId == seed.CheckedInAppointmentId));
    }

    [Fact]
    public void Diagnosis_validation_key_matches_the_nested_consultation_form_field()
    {
        var modelState = new ModelStateDictionary();
        var key = $"{nameof(ConsultationPageViewModel.Form)}.{nameof(ConsultationFormViewModel.Diagnosis)}";

        modelState.AddModelError(key, "Diagnosis is required.");

        var error = Assert.Single(modelState["Form.Diagnosis"]!.Errors);
        Assert.Equal("Diagnosis is required.", error.ErrorMessage);
    }

    [Fact]
    public async Task Finalizing_a_draft_sets_final()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        Assert.True(await consultations.FinalizeAsync(seed.DraftRecordId));

        var record = await db.MedicalRecords.SingleAsync(r => r.Id == seed.DraftRecordId);
        Assert.Equal(EntityStatus.RecordFinal, record.Status);
    }

    [Fact]
    public async Task Medical_record_list_is_newest_first()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var records = await db.MedicalRecords
            .Where(r => r.PatientId == seed.PatientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        Assert.Equal("Diagnosis 1", records[0].Diagnosis);
        Assert.Equal("Diagnosis 5", records[4].Diagnosis);
    }

    [Fact]
    public async Task A_doctor_with_no_appointment_with_the_patient_has_no_access()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();

        Assert.True(await consultations.DoctorHasAppointmentWithPatientAsync(seed.DoctorId, seed.PatientId));
        Assert.False(await consultations.DoctorHasAppointmentWithPatientAsync(seed.OtherDoctorId, seed.PatientId));
    }

    [Fact]
    public async Task Editing_a_final_record_uses_the_owning_doctors_user_name_for_the_audit()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        await consultations.SaveDraftAsync(new MedicalRecord
        {
            Id = seed.FinalRecordId,
            AppointmentId = seed.FinalAppointmentId,
            PatientId = seed.PatientId,
            DoctorId = seed.OtherDoctorId,
            Diagnosis = "Owner audit diagnosis"
        }, "signed-in-other-doctor@hms.local");

        var entry = await db.AuditEntries.SingleAsync(a =>
            a.RecordType == "MedicalRecord" && a.RecordId == seed.FinalRecordId && a.FieldName == "Diagnosis");
        Assert.Equal(seed.DoctorEmail, entry.UserName);
    }

    [Fact]
    public async Task Editing_a_final_record_keeps_the_old_value_new_value_user_and_time_in_the_audit_trail()
    {
        var seed = await SeedAsync();
        using var scope = _services.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        await consultations.SaveDraftAsync(new MedicalRecord
        {
            Id = seed.FinalRecordId,
            AppointmentId = seed.FinalAppointmentId,
            PatientId = seed.PatientId,
            DoctorId = seed.DoctorId,
            Diagnosis = "Updated diagnosis"
        }, seed.DoctorEmail);

        var record = await db.MedicalRecords.SingleAsync(r => r.Id == seed.FinalRecordId);
        Assert.Equal("Updated diagnosis", record.Diagnosis);
        Assert.Equal(EntityStatus.RecordFinal, record.Status);

        var entry = await db.AuditEntries.SingleAsync(a =>
            a.RecordType == "MedicalRecord" && a.RecordId == seed.FinalRecordId && a.FieldName == "Diagnosis");
        Assert.Equal("Diagnosis 1", entry.OldValue);
        Assert.Equal("Updated diagnosis", entry.NewValue);
        Assert.Equal(seed.DoctorEmail, entry.UserName);
        Assert.True(entry.ChangedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    private sealed record SeedData(
        int PatientId,
        int DoctorId,
        int OtherDoctorId,
        string DoctorEmail,
        int CheckedInAppointmentId,
        int FinalAppointmentId,
        int FinalRecordId,
        int DraftRecordId);

    private async Task<SeedData> SeedAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var now = DateTime.UtcNow;

        var department = new Department { Name = $"Cardiology {Guid.NewGuid():N}", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = "Dr. Test",
            Email = "doctor@hms.local",
            Phone = "9000000001",
            Specialization = "Cardiology",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var otherDoctor = new Doctor
        {
            FullName = "Dr. Other",
            Email = "other@hms.local",
            Phone = "9000000002",
            Specialization = "Cardiology",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Doctors.AddRange(doctor, otherDoctor);
        await db.SaveChangesAsync();

        var patient = new Patient
        {
            PatientCode = $"P-{Guid.NewGuid():N}"[..10],
            FullName = "Test Patient",
            DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            Phone = "9000000003",
            Allergies = "Penicillin",
            MedicalHistory = "Hypertension since 2015",
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        // Six past consultations with this Doctor, oldest first so Diagnosis 1 is newest.
        var records = new List<MedicalRecord>();
        for (var i = 1; i <= 6; i++)
        {
            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDate = now.Date.AddDays(-i),
                StartTime = new TimeSpan(9, 0, 0),
                Status = EntityStatus.AppointmentCompleted,
                CreatedAt = now.AddDays(-i),
                UpdatedAt = now
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var record = new MedicalRecord
            {
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = $"Diagnosis {i}",
                Status = EntityStatus.RecordFinal,
                CreatedAt = now.AddDays(-i),
                UpdatedAt = now
            };
            db.MedicalRecords.Add(record);
            records.Add(record);
        }
        await db.SaveChangesAsync();

        // A Checked In appointment with no record yet, and a Draft record to finalize.
        var checkedIn = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = now.Date,
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentCheckedIn,
            CreatedAt = now,
            UpdatedAt = now
        };
        var draftAppointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = now.Date.AddDays(-7),
            StartTime = new TimeSpan(11, 0, 0),
            Status = EntityStatus.AppointmentInProgress,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now
        };
        db.Appointments.AddRange(checkedIn, draftAppointment);
        await db.SaveChangesAsync();

        var draft = new MedicalRecord
        {
            AppointmentId = draftAppointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Draft diagnosis",
            Status = EntityStatus.RecordDraft,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now
        };
        db.MedicalRecords.Add(draft);
        await db.SaveChangesAsync();

        // The newest record (Diagnosis 1) is the Final record edited by the audit test.
        var finalRecord = records[0];

        return new SeedData(
            patient.Id,
            doctor.Id,
            otherDoctor.Id,
            doctor.Email,
            checkedIn.Id,
            finalRecord.AppointmentId,
            finalRecord.Id,
            draft.Id);
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
