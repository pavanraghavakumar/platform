using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests.AppointmentWorkflow;

using AppointmentEntity = Hms.Web.Models.Entities.Appointment;

/// <summary>
/// Appointment booking and status workflow (Requirement 5). Each test boots the real app on its own
/// throwaway SQLite file in the temp folder, seeds only the rows it needs and asserts only on those rows.
/// </summary>
public class AppointmentWorkflowTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"hms-appointment-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;

    private const string ReceptionistEmail = "reception.appt@hms.local";
    private const string ReceptionistPassword = "Reception@123";
    private const string AdminEmail = "admin.appt@hms.local";
    private const string AdminPassword = "Admin@123";
    private const string PatientEmail = "patient.appt@hms.local";
    private const string PatientPassword = "Patient@123";
    private const string OtherPatientEmail = "patient.other@hms.local";
    private const string DoctorEmail = "doctor.appt@hms.local";
    private const string DoctorPassword = "Doctor@123";
    private const string OtherDoctorEmail = "doctor.other.appt@hms.local";

    private int _doctorId;
    private int _patientId;
    private int _otherPatientId;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<HmsDbContext>>();
                services.RemoveAll<HmsDbContext>();
                services.AddDbContext<HmsDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
            });
        });

        // Booting the client runs the migrations and the demo seeder.
        using var client = _factory.CreateClient();
        await SeedAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Slots_returns_only_free_slots_inside_the_doctors_working_hours()
    {
        var date = NextWorkingDay();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            db.Appointments.Add(new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(10, 0, 0),
                Status = EntityStatus.AppointmentConfirmed
            });
            await db.SaveChangesAsync();
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        var response = await client.GetAsync($"/Appointment/Slots?doctorId={_doctorId}&date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"time\":\"09:00\",\"available\":true", body);
        Assert.DoesNotContain("\"time\":\"10:00\"", body);
        Assert.Contains("\"time\":\"16:30\",\"available\":true", body);
        // 17:00 is the end of the working hours, so it is not a slot.
        Assert.DoesNotContain("\"time\":\"17:00\"", body);
    }

    [Fact]
    public void Slot_picker_ignores_responses_from_superseded_requests()
    {
        var script = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "Hms.Web", "wwwroot", "js", "pages", "appointment.js"));

        Assert.Contains("var requestGeneration = 0;", script);
        Assert.Contains("var generation = ++requestGeneration;", script);
        Assert.Contains("generation === requestGeneration", script);
    }

    [Fact]
    public async Task Booking_creates_a_pending_appointment_and_shows_the_message()
    {
        var date = NextWorkingDay();
        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);

        var response = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = _patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "11:00"
        });

        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Expected a redirect to the appointment list, got {response.StatusCode}.");
        var listPage = await client.GetAsync(response.Headers.Location);
        Assert.Contains("Appointment booked.", await listPage.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var appointment = await db.Appointments.SingleAsync(a => a.DoctorId == _doctorId && a.StartTime == new TimeSpan(11, 0, 0) && a.AppointmentDate == date);
        Assert.Equal(EntityStatus.AppointmentPending, appointment.Status);
        Assert.Equal(_patientId, appointment.PatientId);
    }

    [Fact]
    public async Task Booking_a_taken_slot_shows_the_conflict_message_and_creates_nothing()
    {
        var date = NextWorkingDay();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            db.Appointments.Add(new AppointmentEntity
            {
                PatientId = _otherPatientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(12, 0, 0),
                Status = EntityStatus.AppointmentPending
            });
            await db.SaveChangesAsync();
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        var response = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = _patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "12:00"
        });

        Assert.Contains("This time slot is already booked.", await response.Content.ReadAsStringAsync());

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<HmsDbContext>();
        var count = await db2.Appointments.CountAsync(a => a.DoctorId == _doctorId && a.StartTime == new TimeSpan(12, 0, 0) && a.AppointmentDate == date);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Booking_rejects_a_time_that_is_not_a_generated_half_hour_slot()
    {
        var date = NextWorkingDay();
        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);

        var response = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = _patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "11:15"
        });

        Assert.Contains("This time slot is already booked.", await response.Content.ReadAsStringAsync());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await db.Appointments.AnyAsync(a => a.DoctorId == _doctorId
            && a.AppointmentDate == date && a.StartTime == new TimeSpan(11, 15, 0)));
    }

    [Fact]
    public async Task Database_rejects_duplicate_blocking_appointments_for_the_same_slot()
    {
        var date = NextWorkingDay();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        db.Appointments.AddRange(
            new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(12, 30, 0),
                Status = EntityStatus.AppointmentPending
            },
            new AppointmentEntity
            {
                PatientId = _otherPatientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(12, 30, 0),
                Status = EntityStatus.AppointmentConfirmed
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Booking_translates_a_sqlite_unique_slot_race_to_the_conflict_message()
    {
        var date = NextWorkingDay();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            await db.Database.ExecuteSqlRawAsync($"""
                CREATE TRIGGER SimulateConcurrentAppointmentBooking
                BEFORE INSERT ON Appointments
                WHEN NEW.DoctorId = {_doctorId} AND NEW.StartTime = '13:30:00'
                BEGIN
                    INSERT INTO Appointments
                        (PatientId, DoctorId, AppointmentDate, StartTime, Status, Notes, CreatedAt, UpdatedAt)
                    VALUES
                        (NEW.PatientId, NEW.DoctorId, NEW.AppointmentDate, NEW.StartTime, NEW.Status, NEW.Notes, NEW.CreatedAt, NEW.UpdatedAt);
                END;
                """);
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        var response = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = _patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "13:30"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This time slot is already booked.", await response.Content.ReadAsStringAsync());

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await verificationDb.Appointments.AnyAsync(a => a.DoctorId == _doctorId
            && a.AppointmentDate == date && a.StartTime == new TimeSpan(13, 30, 0)));
    }

    [Fact]
    public async Task Existing_database_is_upgraded_to_the_filtered_unique_slot_index()
    {
        var upgradeDatabasePath = Path.Combine(Path.GetTempPath(), $"hms-appointment-upgrade-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<HmsDbContext>()
                .UseSqlite($"Data Source={upgradeDatabasePath}")
                .Options;

            await using var db = new HmsDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260101000000_InitialCreate");

            Assert.Equal(0L, await ReadAppointmentSlotIndexUniqueFlagAsync(db));

            await migrator.MigrateAsync();

            Assert.Equal(1L, await ReadAppointmentSlotIndexUniqueFlagAsync(db));
            var indexSql = await ReadAppointmentSlotIndexSqlAsync(db);
            Assert.Contains("WHERE \"Status\" IN ('Pending', 'Confirmed', 'Checked In', 'In Progress')", indexSql);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(upgradeDatabasePath))
            {
                File.Delete(upgradeDatabasePath);
            }
        }
    }

    [Fact]
    public async Task Existing_database_with_duplicate_blocking_slots_is_remediated_before_upgrade()
    {
        var upgradeDatabasePath = Path.Combine(Path.GetTempPath(), $"hms-appointment-duplicate-upgrade-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<HmsDbContext>()
                .UseSqlite($"Data Source={upgradeDatabasePath}")
                .Options;

            await using var db = new HmsDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260101000000_InitialCreate");

            var department = new Department { Name = $"Upgrade {Guid.NewGuid():N}", Status = EntityStatus.Active };
            var doctor = new Doctor
            {
                FullName = "Upgrade Doctor",
                Email = $"upgrade-doctor-{Guid.NewGuid():N}@hms.local",
                Phone = "9000000001",
                Specialization = "General Medicine",
                Qualifications = "MBBS",
                Department = department,
                Status = EntityStatus.Active
            };
            var firstPatient = new Patient
            {
                PatientCode = $"P-{Guid.NewGuid():N}",
                FullName = "First Upgrade Patient",
                Phone = "9000000002",
                Status = EntityStatus.Active
            };
            var secondPatient = new Patient
            {
                PatientCode = $"P-{Guid.NewGuid():N}",
                FullName = "Second Upgrade Patient",
                Phone = "9000000003",
                Status = EntityStatus.Active
            };
            var date = NextWorkingDay();
            var time = new TimeSpan(14, 0, 0);
            var winner = new AppointmentEntity
            {
                Patient = firstPatient,
                Doctor = doctor,
                AppointmentDate = date,
                StartTime = time,
                Status = EntityStatus.AppointmentConfirmed
            };
            var duplicate = new AppointmentEntity
            {
                Patient = secondPatient,
                Doctor = doctor,
                AppointmentDate = date,
                StartTime = time,
                Status = EntityStatus.AppointmentCheckedIn
            };
            db.Appointments.AddRange(winner, duplicate);
            await db.SaveChangesAsync();
            Assert.True(winner.Id < duplicate.Id);

            await migrator.MigrateAsync();

            db.ChangeTracker.Clear();
            Assert.Equal(EntityStatus.AppointmentConfirmed,
                await db.Appointments.Where(a => a.Id == winner.Id).Select(a => a.Status).SingleAsync());
            Assert.Equal(EntityStatus.AppointmentCancelled,
                await db.Appointments.Where(a => a.Id == duplicate.Id).Select(a => a.Status).SingleAsync());
            Assert.Equal(1L, await ReadAppointmentSlotIndexUniqueFlagAsync(db));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(upgradeDatabasePath))
            {
                File.Delete(upgradeDatabasePath);
            }
        }
    }

    [Fact]
    public async Task A_patient_cannot_book_for_another_patient()
    {
        var date = NextWorkingDay();
        using var client = await SignedInClientAsync(PatientEmail, PatientPassword);

        var response = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = _otherPatientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "13:00"
        });

        Assert.Contains("You can only book appointments for yourself.", await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await db.Appointments.AnyAsync(a => a.DoctorId == _doctorId && a.StartTime == new TimeSpan(13, 0, 0) && a.AppointmentDate == date));
    }

    [Fact]
    public async Task Admin_can_view_the_list_without_receptionist_action_buttons()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(13, 30, 0),
                Status = EntityStatus.AppointmentPending
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(AdminEmail, AdminPassword);
        var response = await client.GetAsync("/Appointment/Index");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain($"/Appointment/Confirm/{appointmentId}", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain($"/Appointment/Cancel/{appointmentId}", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Confirm_notifies_the_patient_and_check_in_notifies_the_doctor()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(14, 0, 0),
                Status = EntityStatus.AppointmentPending
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        await PostFormAsync(client, $"/Appointment/Confirm/{appointmentId}", new Dictionary<string, string>());
        await PostFormAsync(client, $"/Appointment/CheckIn/{appointmentId}", new Dictionary<string, string>());

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<HmsDbContext>();
        var updated = await db2.Appointments.SingleAsync(a => a.Id == appointmentId);
        Assert.Equal(EntityStatus.AppointmentCheckedIn, updated.Status);

        var patientUserId = await db2.Patients.Where(p => p.Id == _patientId).Select(p => p.UserId).SingleAsync();
        var doctorUserId = await db2.Doctors.Where(d => d.Id == _doctorId).Select(d => d.UserId).SingleAsync();
        Assert.True(await db2.Notifications.AnyAsync(n => n.UserId == patientUserId));
        Assert.True(await db2.Notifications.AnyAsync(n => n.UserId == doctorUserId));
    }

    [Fact]
    public async Task Receptionist_registered_patient_has_an_account_that_receives_confirmation_notifications()
    {
        var email = $"registered.{Guid.NewGuid():N}@hms.local";
        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        var registration = await PostFormAsync(client, "/Patient/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Registered Appointment Patient",
            ["Phone"] = "9888888888",
            ["Email"] = email,
            ["Password"] = PatientPassword
        }, "/Patient/Create");
        Assert.Equal(HttpStatusCode.Redirect, registration.StatusCode);

        int patientId;
        string patientUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var patient = await db.Patients.SingleAsync(p => p.Email == email);
            Assert.False(string.IsNullOrWhiteSpace(patient.UserId));
            patientId = patient.Id;
            patientUserId = patient.UserId!;
        }

        var date = NextWorkingDay();
        var booking = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "09:30"
        });
        Assert.Equal(HttpStatusCode.Redirect, booking.StatusCode);

        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            appointmentId = await db.Appointments
                .Where(a => a.PatientId == patientId && a.AppointmentDate == date)
                .Select(a => a.Id)
                .SingleAsync();
        }

        await PostFormAsync(client, $"/Appointment/Confirm/{appointmentId}", new Dictionary<string, string>());

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.True(await verificationDb.Notifications.AnyAsync(n => n.UserId == patientUserId
            && n.Message.Contains("is confirmed")));
    }

    [Fact]
    public async Task Receptionist_can_register_and_book_a_patient_without_portal_credentials()
    {
        const string phone = "9888888877";
        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);

        var registration = await PostFormAsync(client, "/Patient/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Patient Without Portal Account",
            ["Phone"] = phone
        }, "/Patient/Create");

        Assert.Equal(HttpStatusCode.Redirect, registration.StatusCode);

        int patientId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var patient = await db.Patients.SingleAsync(p => p.Phone == phone);
            Assert.Null(patient.UserId);
            patientId = patient.Id;
        }

        var bookPage = await client.GetStringAsync("/Appointment/Book");
        Assert.Contains($"value=\"{patientId}\"", bookPage, StringComparison.OrdinalIgnoreCase);

        var date = NextWorkingDay();
        var booking = await PostFormAsync(client, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "10:30"
        });

        Assert.Equal(HttpStatusCode.Redirect, booking.StatusCode);
        var listPage = await client.GetAsync(booking.Headers.Location);
        Assert.Contains("Appointment booked.", await listPage.Content.ReadAsStringAsync());

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var appointment = await verificationDb.Appointments.SingleAsync(a => a.PatientId == patientId
            && a.DoctorId == _doctorId && a.AppointmentDate == date && a.StartTime == new TimeSpan(10, 30, 0));
        Assert.Equal(EntityStatus.AppointmentPending, appointment.Status);
    }

    [Theory]
    [InlineData("partial.credentials@hms.local", "")]
    [InlineData("", PatientPassword)]
    public async Task Receptionist_registration_requires_email_and_password_together(string email, string password)
    {
        var phone = $"97{Random.Shared.Next(10000000, 99999999)}";
        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);

        var registration = await PostFormAsync(client, "/Patient/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Patient With Partial Credentials",
            ["Phone"] = phone,
            ["Email"] = email,
            ["Password"] = password
        }, "/Patient/Create");

        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Assert.Contains("Email and sign-in password must be provided together.",
            await registration.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await db.Patients.AnyAsync(p => p.Phone == phone));
    }

    [Fact]
    public async Task Receptionist_can_book_an_existing_active_accountless_patient()
    {
        int patientId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var patient = new Patient
            {
                PatientCode = $"P-{Guid.NewGuid():N}"[..8],
                FullName = "Accountless Patient",
                DateOfBirth = new DateTime(1992, 1, 1),
                Gender = "Female",
                Phone = "9000000099",
                Status = EntityStatus.Active,
                UserId = null
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            patientId = patient.Id;
        }

        using var receptionist = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        var bookPage = await receptionist.GetStringAsync("/Appointment/Book");
        Assert.Contains($"value=\"{patientId}\"", bookPage, StringComparison.OrdinalIgnoreCase);

        var date = NextWorkingDay();
        var booking = await PostFormAsync(receptionist, "/Appointment/Book", new Dictionary<string, string>
        {
            ["PatientId"] = patientId.ToString(),
            ["DoctorId"] = _doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "13:00"
        });
        Assert.Equal(HttpStatusCode.Redirect, booking.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var appointment = await verificationDb.Appointments.SingleAsync(a => a.PatientId == patientId
            && a.DoctorId == _doctorId && a.AppointmentDate == date && a.StartTime == new TimeSpan(13, 0, 0));
        Assert.Equal(EntityStatus.AppointmentPending, appointment.Status);
    }

    [Fact]
    public async Task Receptionist_can_confirm_and_cancel_an_accountless_patients_appointment()
    {
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var patient = new Patient
            {
                PatientCode = $"P-{Guid.NewGuid():N}"[..8],
                FullName = "Accountless Workflow Patient",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Female",
                Phone = "9000000088",
                Status = EntityStatus.Active,
                UserId = null
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var appointment = new AppointmentEntity
            {
                PatientId = patient.Id,
                DoctorId = _doctorId,
                AppointmentDate = NextWorkingDay(),
                StartTime = new TimeSpan(8, 1, 0),
                Status = EntityStatus.AppointmentPending
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using (var confirmScope = _factory.Services.CreateScope())
        {
            var service = confirmScope.ServiceProvider.GetRequiredService<IAppointmentService>();
            Assert.True(await service.ConfirmAsync(appointmentId));
        }

        using (var cancelScope = _factory.Services.CreateScope())
        {
            var service = cancelScope.ServiceProvider.GetRequiredService<IAppointmentService>();
            Assert.True(await service.CancelAsync(appointmentId, "receptionist", isReceptionist: true));
        }

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentCancelled,
            (await verificationDb.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
    }

    [Fact]
    public async Task Concurrent_confirm_requests_allow_one_transition_and_one_notification()
    {
        var appointmentId = await CreateAppointmentAsync(EntityStatus.AppointmentPending, new TimeSpan(14, 30, 0));

        using var firstScope = _factory.Services.CreateScope();
        using var secondScope = _factory.Services.CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IAppointmentService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IAppointmentService>();

        var results = await Task.WhenAll(
            firstService.ConfirmAsync(appointmentId),
            secondService.ConfirmAsync(appointmentId));

        Assert.Single(results.Where(result => result));

        using var verificationScope = _factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentConfirmed,
            (await db.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);

        var patientUserId = await db.Patients
            .Where(p => p.Id == _patientId)
            .Select(p => p.UserId)
            .SingleAsync();
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.UserId == patientUserId
            && n.Message.Contains("is confirmed")));
    }

    [Fact]
    public async Task Confirm_rolls_back_status_when_notification_persistence_fails()
    {
        var appointmentId = await CreateAppointmentAsync(EntityStatus.AppointmentPending, new TimeSpan(14, 30, 0));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var service = new AppointmentService(db, new FailingNotificationService());

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(appointmentId));
        }

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentPending,
            (await verificationDb.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
    }

    [Fact]
    public async Task Cancel_frees_the_slot_at_once()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(15, 0, 0),
                Status = EntityStatus.AppointmentConfirmed
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        await PostFormAsync(client, $"/Appointment/Cancel/{appointmentId}", new Dictionary<string, string>());

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<HmsDbContext>();
        var updated = await db2.Appointments.SingleAsync(a => a.Id == appointmentId);
        Assert.Equal(EntityStatus.AppointmentCancelled, updated.Status);

        var slots = await scope2.ServiceProvider.GetRequiredService<IAppointmentService>().GetFreeSlotsAsync(_doctorId, date);
        Assert.Contains(slots, s => s.Time == "15:00" && s.Available);
    }

    [Fact]
    public async Task No_show_frees_the_slot()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(15, 30, 0),
                Status = EntityStatus.AppointmentConfirmed
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(ReceptionistEmail, ReceptionistPassword);
        await PostFormAsync(client, $"/Appointment/NoShow/{appointmentId}", new Dictionary<string, string>());

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<HmsDbContext>();
        var updated = await db2.Appointments.SingleAsync(a => a.Id == appointmentId);
        Assert.Equal(EntityStatus.AppointmentNoShow, updated.Status);

        var slots = await scope2.ServiceProvider.GetRequiredService<IAppointmentService>().GetFreeSlotsAsync(_doctorId, date);
        Assert.Contains(slots, s => s.Time == "15:30" && s.Available);
    }

    [Fact]
    public async Task Start_and_complete_move_a_checked_in_appointment_to_completed()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(16, 0, 0),
                Status = EntityStatus.AppointmentCheckedIn
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(DoctorEmail, DoctorPassword);
        await PostFormAsync(client, $"/Appointment/Start/{appointmentId}", new Dictionary<string, string>());

        using (var scope2 = _factory.Services.CreateScope())
        {
            var db2 = scope2.ServiceProvider.GetRequiredService<HmsDbContext>();
            Assert.Equal(EntityStatus.AppointmentInProgress, (await db2.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
        }

        await PostFormAsync(client, $"/Appointment/Complete/{appointmentId}", new Dictionary<string, string>());

        using var scope3 = _factory.Services.CreateScope();
        var db3 = scope3.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentCompleted, (await db3.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
    }

    [Fact]
    public async Task Doctor_cannot_start_or_complete_another_doctors_appointment()
    {
        var date = NextWorkingDay();
        int appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = new AppointmentEntity
            {
                PatientId = _patientId,
                DoctorId = _doctorId,
                AppointmentDate = date,
                StartTime = new TimeSpan(16, 30, 0),
                Status = EntityStatus.AppointmentCheckedIn
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        using var client = await SignedInClientAsync(OtherDoctorEmail, DoctorPassword);
        var start = await PostFormAsync(client, $"/Appointment/Start/{appointmentId}", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Forbidden, start.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var appointment = await db.Appointments.SingleAsync(a => a.Id == appointmentId);
            Assert.Equal(EntityStatus.AppointmentCheckedIn, appointment.Status);
            appointment.Status = EntityStatus.AppointmentInProgress;
            await db.SaveChangesAsync();
        }

        var complete = await PostFormAsync(client, $"/Appointment/Complete/{appointmentId}", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Forbidden, complete.StatusCode);

        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentInProgress,
            (await finalDb.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
    }

    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var department = new Department { Name = $"Appointments {Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctorUser = await CreateUserAsync(users, DoctorEmail, DoctorPassword, "Dr. Appointment", Roles.Doctor);
        var otherDoctorUser = await CreateUserAsync(users, OtherDoctorEmail, DoctorPassword, "Dr. Other", Roles.Doctor);
        var patientUser = await CreateUserAsync(users, PatientEmail, PatientPassword, "Appointment Patient", Roles.Patient);
        var otherPatientUser = await CreateUserAsync(users, OtherPatientEmail, PatientPassword, "Other Patient", Roles.Patient);
        await CreateUserAsync(users, ReceptionistEmail, ReceptionistPassword, "Appointment Receptionist", Roles.Receptionist);
        await CreateUserAsync(users, AdminEmail, AdminPassword, "Appointment Admin", Roles.Admin);

        var doctor = new Doctor
        {
            FullName = "Dr. Appointment",
            Email = DoctorEmail,
            Phone = "9000000001",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            UserId = doctorUser.Id
        };
        db.Doctors.Add(doctor);
        db.Doctors.Add(new Doctor
        {
            FullName = "Dr. Other",
            Email = OtherDoctorEmail,
            Phone = "9000000004",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            UserId = otherDoctorUser.Id
        });

        var patient = new Patient
        {
            PatientCode = $"P-{Guid.NewGuid():N}"[..8],
            FullName = "Appointment Patient",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            Phone = "9000000002",
            Status = EntityStatus.Active,
            UserId = patientUser.Id
        };
        var otherPatient = new Patient
        {
            PatientCode = $"P-{Guid.NewGuid():N}"[..8],
            FullName = "Other Patient",
            DateOfBirth = new DateTime(1991, 1, 1),
            Gender = "Female",
            Phone = "9000000003",
            Status = EntityStatus.Active,
            UserId = otherPatientUser.Id
        };
        db.Patients.AddRange(patient, otherPatient);
        await db.SaveChangesAsync();

        // Working hours Monday to Saturday, 09:00 to 17:00.
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Saturday; day++)
        {
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = day,
                IsWorking = true,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });
        }

        await db.SaveChangesAsync();

        _doctorId = doctor.Id;
        _patientId = patient.Id;
        _otherPatientId = otherPatient.Id;
    }

    private async Task<int> CreateAppointmentAsync(string status, TimeSpan startTime)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var appointment = new AppointmentEntity
        {
            PatientId = _patientId,
            DoctorId = _doctorId,
            AppointmentDate = NextWorkingDay(),
            StartTime = startTime,
            Status = status
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment.Id;
    }

    private sealed class FailingNotificationService : INotificationService
    {
        public Task AddAsync(string userId, string message)
        {
            throw new InvalidOperationException("Simulated notification persistence failure.");
        }

        public Task<IReadOnlyList<Hms.Web.Models.Entities.Notification>> GetRecentAsync(string userId, int take = 10)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            throw new NotSupportedException();
        }

        public Task MarkAllReadAsync(string userId)
        {
            throw new NotSupportedException();
        }
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> users, string email, string password, string fullName, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };
        var result = await users.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
        return user;
    }

    private async Task<HttpClient> SignedInClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());

        var response = await PostFormAsync(client, "/Account/Login", new Dictionary<string, string>
        {
            ["UserName"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        });

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.OK,
            $"Sign-in for {email} failed with {response.StatusCode}.");
        return client;
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string url,
        Dictionary<string, string> fields,
        string? tokenPageOverride = null)
    {
        // The token is rendered by a page that holds a form; the POST-only action URLs render none.
        var tokenPage = tokenPageOverride ?? (url.StartsWith("/Appointment/Book", StringComparison.OrdinalIgnoreCase)
            ? "/Appointment/Book"
            : "/Appointment/Index");
        var page = await client.GetAsync(tokenPage);
        var html = await page.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);
        if (token is not null)
        {
            fields["__RequestVerificationToken"] = token;
        }

        var form = new FormUrlEncodedContent(fields);
        form.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        return await client.PostAsync(url, form);
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        // The tag helper emits the hidden input with value before name, so match either order.
        var input = Regex.Match(html, "<input[^>]*__RequestVerificationToken[^>]*>");
        if (!input.Success)
        {
            return null;
        }

        var value = Regex.Match(input.Value, "value=\"([^\"]+)\"");
        return value.Success ? value.Groups[1].Value : null;
    }

    private static DateTime NextWorkingDay()
    {
        var date = DateTime.Today.AddDays(1);
        while (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static async Task<long> ReadAppointmentSlotIndexUniqueFlagAsync(HmsDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT [unique] FROM pragma_index_list('Appointments') WHERE name = 'IX_Appointments_DoctorId_AppointmentDate_StartTime'";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<string> ReadAppointmentSlotIndexSqlAsync(HmsDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Appointments_DoctorId_AppointmentDate_StartTime'";
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
    }
}
