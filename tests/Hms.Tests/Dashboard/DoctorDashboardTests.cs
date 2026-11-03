using System.Net;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using NotificationEntity = Hms.Web.Models.Entities.Notification;

namespace Hms.Tests.Dashboard;

public class DoctorDashboardTests : IClassFixture<DoctorDashboardTests.DoctorDashboardFactory>
{
    private readonly DoctorDashboardFactory _factory;

    public DoctorDashboardTests(DoctorDashboardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_shows_doctor_scoped_kpis_and_todays_appointments_earliest_first()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var doctor = await DemoDoctorAsync(db);
        var patient = NewPatient(marker, "Dashboard Patient");
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        db.Appointments.AddRange(
            NewAppointment(doctor.Id, patient.Id, today, 11, EntityStatus.AppointmentCompleted),
            NewAppointment(doctor.Id, patient.Id, today, 8, EntityStatus.AppointmentCheckedIn));
        await db.SaveChangesAsync();

        var labTestId = await db.LaboratoryTests.Select(t => t.Id).FirstAsync();
        db.LabRequests.Add(new LabRequest
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            LaboratoryTestId = labTestId,
            Status = EntityStatus.LabCompleted,
            ResultEnteredAt = today.AddHours(7)
        });
        await db.SaveChangesAsync();

        var expectedAppointments = await db.Appointments.CountAsync(a => a.DoctorId == doctor.Id
            && a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1));
        var expectedWaiting = await db.Appointments.CountAsync(a => a.DoctorId == doctor.Id
            && a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1)
            && a.Status == EntityStatus.AppointmentCheckedIn);
        var expectedCompleted = await db.MedicalRecords.CountAsync(r => r.DoctorId == doctor.Id
            && r.Status == EntityStatus.RecordFinal
            && r.UpdatedAt >= today && r.UpdatedAt < today.AddDays(1));
        var expectedLabs = await db.LabRequests.CountAsync(r => r.DoctorId == doctor.Id
            && r.Status == EntityStatus.LabCompleted
            && r.ResultEnteredAt >= today && r.ResultEnteredAt < today.AddDays(1));

        var html = await GetDoctorPageAsync("/Dashboard/Doctor");

        AssertKpi(html, "Today&#x27;s appointments", expectedAppointments);
        AssertKpi(html, "Checked In waiting", expectedWaiting);
        AssertKpi(html, "Completed consultations today", expectedCompleted);
        AssertKpi(html, "Lab results completed today", expectedLabs);
        Assert.Contains($"Dashboard Patient {marker}", html);
        Assert.Contains($"P-{marker}", html);
        Assert.Contains("Start consultation", html);
        Assert.True(
            html.IndexOf("08:00", StringComparison.Ordinal) < html.IndexOf("11:00", StringComparison.Ordinal),
            "Today's appointments should be earliest first.");
    }

    [Fact]
    public async Task Finalizing_consultation_increases_completed_consultations_kpi_without_completed_appointment()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        int appointmentId;
        int recordId;
        int completedBefore;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var doctor = await DemoDoctorAsync(db);
            var patient = NewPatient($"FINAL-{marker}", "Final Patient");
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var appointment = NewAppointment(
                doctor.Id,
                patient.Id,
                today.AddDays(-2),
                9,
                EntityStatus.AppointmentInProgress);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var record = new MedicalRecord
            {
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = $"Dashboard regression {marker}",
                Status = EntityStatus.RecordDraft,
                CreatedAt = today.AddDays(-2),
                UpdatedAt = today.AddDays(-2)
            };
            db.MedicalRecords.Add(record);
            await db.SaveChangesAsync();

            appointmentId = appointment.Id;
            recordId = record.Id;
            completedBefore = await db.MedicalRecords.CountAsync(r => r.DoctorId == doctor.Id
                && r.Status == EntityStatus.RecordFinal
                && r.UpdatedAt >= today && r.UpdatedAt < today.AddDays(1));
        }

        var client = await SignedInDoctorClientAsync();
        var consultation = await client.GetStringAsync($"/Consultation?appointmentId={appointmentId}");
        var form = Regex.Match(
            consultation,
            $"<form[^>]*action=\"/Consultation/Finalize/{recordId}\".*?</form>",
            RegexOptions.Singleline);
        Assert.True(form.Success, "The Draft consultation should have a Finalize form.");
        var token = Regex.Match(form.Value, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The Finalize form should carry an anti-forgery token.");

        var response = await client.PostAsync(
            $"/Consultation/Finalize/{recordId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var dashboard = await client.GetStringAsync("/Dashboard/Doctor");
        AssertKpi(dashboard, "Completed consultations today", completedBefore + 1);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.RecordFinal, (await verifyDb.MedicalRecords.FindAsync(recordId))!.Status);
        Assert.Equal(EntityStatus.AppointmentInProgress, (await verifyDb.Appointments.FindAsync(appointmentId))!.Status);
    }

    [Fact]
    public async Task Dashboard_shows_only_five_upcoming_appointments_and_notifications_in_order()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var doctor = await DemoDoctorAsync(db);
        var userId = doctor.UserId!;
        var created = DateTime.UtcNow.AddDays(1);

        for (var i = 0; i < 6; i++)
        {
            var patient = NewPatient($"{marker}-{i}", $"Upcoming {i}");
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            db.Appointments.Add(NewAppointment(
                doctor.Id,
                patient.Id,
                today.AddDays(i + 1),
                6,
                EntityStatus.AppointmentConfirmed));
            db.Notifications.Add(new NotificationEntity
            {
                UserId = userId,
                Message = $"Doctor notice {marker} {i}",
                CreatedAt = created.AddMinutes(i),
                UpdatedAt = created.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var html = await GetDoctorPageAsync("/Dashboard/Doctor");

        Assert.Contains($"Upcoming 0 {marker}-0", html);
        Assert.DoesNotContain($"Upcoming 5 {marker}-5", html);
        Assert.Contains($"Doctor notice {marker} 5", html);
        Assert.DoesNotContain($"Doctor notice {marker} 0", html);
        Assert.True(
            html.IndexOf($"Doctor notice {marker} 5", StringComparison.Ordinal)
            < html.IndexOf($"Doctor notice {marker} 4", StringComparison.Ordinal),
            "Notifications should be newest first.");
    }

    [Fact]
    public async Task My_patients_shows_only_patients_with_this_doctor_and_last_appointment()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var doctor = await DemoDoctorAsync(db);
        var departmentId = await db.Departments.Select(d => d.Id).FirstAsync();
        var otherDoctor = new Doctor
        {
            FullName = $"Other Doctor {marker}",
            Email = $"other.{marker}@hms.local",
            Phone = "9000000300",
            Specialization = "General",
            DepartmentId = departmentId
        };
        var ownPatient = NewPatient($"OWN-{marker}", "Own Patient");
        var otherPatient = NewPatient($"OTHER-{marker}", "Other Patient");
        db.AddRange(otherDoctor, ownPatient, otherPatient);
        await db.SaveChangesAsync();

        db.Appointments.AddRange(
            NewAppointment(doctor.Id, ownPatient.Id, today.AddDays(-5), 9, EntityStatus.AppointmentCompleted),
            NewAppointment(doctor.Id, ownPatient.Id, today.AddDays(-1), 10, EntityStatus.AppointmentCompleted),
            NewAppointment(otherDoctor.Id, otherPatient.Id, today, 11, EntityStatus.AppointmentConfirmed));
        await db.SaveChangesAsync();

        var html = await GetDoctorPageAsync("/Patient/DoctorPatients");

        Assert.Contains($"P-OWN-{marker}", html);
        Assert.Contains($"Own Patient OWN-{marker}", html);
        Assert.Contains(today.AddDays(-1).ToString("dd MMM yyyy"), html);
        Assert.DoesNotContain($"P-OTHER-{marker}", html);
        Assert.DoesNotContain($"Other Patient OTHER-{marker}", html);
    }

    [Fact]
    public async Task Patient_details_rejects_doctor_without_an_appointment_with_patient()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        int patientId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var departmentId = await db.Departments.Select(d => d.Id).FirstAsync();
            var otherDoctor = new Doctor
            {
                FullName = $"Patient Owner Doctor {marker}",
                Email = $"patient.owner.{marker}@hms.local",
                Phone = "9000000301",
                Specialization = "General",
                DepartmentId = departmentId
            };
            var patient = NewPatient($"PRIVATE-{marker}", "Private Patient");
            db.AddRange(otherDoctor, patient);
            await db.SaveChangesAsync();

            db.Appointments.Add(NewAppointment(
                otherDoctor.Id,
                patient.Id,
                DateTime.UtcNow.Date,
                12,
                EntityStatus.AppointmentConfirmed));
            await db.SaveChangesAsync();
            patientId = patient.Id;
        }

        var client = await SignedInDoctorClientAsync();
        var response = await client.GetAsync($"/Patient/Details/{patientId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Start_consultation_transitions_checked_in_appointment_and_opens_consultation()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        int appointmentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var doctor = await DemoDoctorAsync(db);
            var patient = NewPatient($"START-{marker}", "Start Patient");
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            var appointment = NewAppointment(
                doctor.Id,
                patient.Id,
                DateTime.UtcNow.Date,
                7,
                EntityStatus.AppointmentCheckedIn);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        var client = await SignedInDoctorClientAsync();
        var dashboard = await client.GetStringAsync("/Dashboard/Doctor");
        var form = Regex.Match(
            dashboard,
            $"<form[^>]*action=\"/Appointment/Start/{appointmentId}\".*?</form>",
            RegexOptions.Singleline);
        Assert.True(form.Success, "The Checked In appointment should have a Start consultation form.");
        var token = Regex.Match(form.Value, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The Start consultation form should carry an anti-forgery token.");

        var response = await client.PostAsync(
            $"/Appointment/Start/{appointmentId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Consultation?appointmentId={appointmentId}", response.Headers.Location?.OriginalString);
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentInProgress, (await verifyDb.Appointments.FindAsync(appointmentId))!.Status);
    }

    private async Task<string> GetDoctorPageAsync(string path)
    {
        var client = await SignedInDoctorClientAsync();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> SignedInDoctorClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The sign-in page should carry an anti-forgery token.");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userName"] = DemoDataSeeder.DoctorEmail,
            ["password"] = DemoDataSeeder.DoctorPassword,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static void AssertKpi(string html, string label, int value)
    {
        Assert.Matches(
            $"<div class=\"hms-kpi-value\">{value}</div>\\s*<div class=\"hms-kpi-label\">{label}</div>",
            html);
    }

    private static Task<Doctor> DemoDoctorAsync(HmsDbContext db) =>
        db.Doctors.SingleAsync(d => d.Email == DemoDataSeeder.DoctorEmail);

    private static Patient NewPatient(string marker, string name) => new()
    {
        PatientCode = $"P-{marker}",
        FullName = $"{name} {marker}",
        Phone = "9000000200",
        Gender = "Female"
    };

    private static Appointment NewAppointment(int doctorId, int patientId, DateTime date, int hour, string status) => new()
    {
        DoctorId = doctorId,
        PatientId = patientId,
        AppointmentDate = date,
        StartTime = new TimeSpan(hour, 0, 0),
        Status = status
    };

    public sealed class DoctorDashboardFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath =
            Path.Combine(Path.GetTempPath(), $"hms-doctor-dashboard-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
        }
    }
}
