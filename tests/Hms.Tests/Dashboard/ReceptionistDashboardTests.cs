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

public class ReceptionistDashboardTests : IClassFixture<ReceptionistDashboardTests.ReceptionistDashboardFactory>
{
    private readonly ReceptionistDashboardFactory _factory;

    public ReceptionistDashboardTests(ReceptionistDashboardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_shows_database_kpis_appointments_earliest_first_and_patient_actions()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        int confirmedId;
        int expectedToday;
        int expectedPending;
        int expectedCheckedIn;
        decimal expectedPayments;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var doctor = await db.Doctors.FirstAsync();
            var patient = NewPatient(marker, "Reception Patient");
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var confirmed = NewAppointment(doctor.Id, patient.Id, today, 8, EntityStatus.AppointmentConfirmed);
            var pending = NewAppointment(doctor.Id, patient.Id, today, 11, EntityStatus.AppointmentPending);
            var checkedIn = NewAppointment(doctor.Id, patient.Id, today.AddDays(1), 9, EntityStatus.AppointmentCheckedIn);
            db.Appointments.AddRange(confirmed, pending, checkedIn);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{marker}",
                PatientId = patient.Id,
                AppointmentId = pending.Id,
                Total = 100m,
                PaidAmount = 37.50m,
                Status = EntityStatus.InvoicePartiallyPaid
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            db.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                Amount = 37.50m,
                PaidAt = today.AddHours(10),
                Status = EntityStatus.PaymentRecorded
            });
            await db.SaveChangesAsync();

            confirmedId = confirmed.Id;
            expectedToday = await db.Appointments.CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1));
            expectedPending = await db.Appointments.CountAsync(a => a.Status == EntityStatus.AppointmentPending);
            expectedCheckedIn = await db.Appointments.CountAsync(a => a.Status == EntityStatus.AppointmentCheckedIn);
            expectedPayments = (await db.Payments
                .Where(p => p.Status == EntityStatus.PaymentRecorded && p.PaidAt >= today && p.PaidAt < today.AddDays(1))
                .Select(p => p.Amount)
                .ToListAsync()).Sum();
        }

        var html = await GetReceptionistPageAsync();

        AssertKpi(html, "Appointments today", expectedToday.ToString());
        AssertKpi(html, "Pending appointments", expectedPending.ToString());
        AssertKpi(html, "Checked In appointments", expectedCheckedIn.ToString());
        AssertKpi(html, "Payments recorded today", expectedPayments.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-IN")));
        Assert.Contains($"Reception Patient {marker}", html);
        Assert.Contains($"P-{marker}", html);
        Assert.Contains($"/Patient/Details/", html);
        Assert.Contains("/Invoice/Generate", html);
        Assert.Contains("/Appointment/Book", html);
        Assert.True(html.IndexOf("08:00", StringComparison.Ordinal) < html.IndexOf("11:00", StringComparison.Ordinal));

        var checkInForm = Regex.Match(html, $"<form[^>]*action=\"/Appointment/CheckIn/{confirmedId}\".*?</form>", RegexOptions.Singleline);
        Assert.True(checkInForm.Success, "A Confirmed appointment should show its Check in form.");
        Assert.Contains("Check in", checkInForm.Value);
        Assert.Contains("__RequestVerificationToken", checkInForm.Value);
    }

    [Fact]
    public async Task Dashboard_shows_only_the_receptionists_five_most_recent_notifications()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var receptionistId = await db.Users
            .Where(u => u.Email == DemoDataSeeder.ReceptionistEmail)
            .Select(u => u.Id)
            .SingleAsync();
        var doctorId = await db.Users
            .Where(u => u.Email == DemoDataSeeder.DoctorEmail)
            .Select(u => u.Id)
            .SingleAsync();
        var created = DateTime.UtcNow.AddDays(2);

        for (var i = 0; i < 6; i++)
        {
            db.Notifications.Add(new NotificationEntity
            {
                UserId = receptionistId,
                Message = $"Reception notice {marker} {i}",
                CreatedAt = created.AddMinutes(i),
                UpdatedAt = created.AddMinutes(i)
            });
        }
        db.Notifications.Add(new NotificationEntity
        {
            UserId = doctorId,
            Message = $"Doctor-only notice {marker}",
            CreatedAt = created.AddHours(1),
            UpdatedAt = created.AddHours(1)
        });
        await db.SaveChangesAsync();

        var html = await GetReceptionistPageAsync();

        Assert.Contains($"Reception notice {marker} 5", html);
        Assert.DoesNotContain($"Reception notice {marker} 0", html);
        Assert.DoesNotContain($"Doctor-only notice {marker}", html);
        Assert.True(
            html.IndexOf($"Reception notice {marker} 5", StringComparison.Ordinal)
            < html.IndexOf($"Reception notice {marker} 4", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Check_in_from_dashboard_transitions_the_confirmed_appointment()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        int appointmentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var doctor = await db.Doctors.FirstAsync();
            var patient = NewPatient($"CHECK-{marker}", "Check In Patient");
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            var appointment = NewAppointment(doctor.Id, patient.Id, DateTime.UtcNow.Date, 7, EntityStatus.AppointmentConfirmed);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        var client = await SignedInReceptionistClientAsync();
        var dashboard = await client.GetStringAsync("/Dashboard/Receptionist");
        var form = Regex.Match(
            dashboard,
            $"<form[^>]*action=\"/Appointment/CheckIn/{appointmentId}\".*?</form>",
            RegexOptions.Singleline);
        Assert.True(form.Success);
        var token = Regex.Match(form.Value, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);

        var response = await client.PostAsync(
            $"/Appointment/CheckIn/{appointmentId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentCheckedIn, (await verifyDb.Appointments.FindAsync(appointmentId))!.Status);
    }

    private async Task<string> GetReceptionistPageAsync()
    {
        var client = await SignedInReceptionistClientAsync();
        var response = await client.GetAsync("/Dashboard/Receptionist");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> SignedInReceptionistClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userName"] = DemoDataSeeder.ReceptionistEmail,
            ["password"] = DemoDataSeeder.ReceptionistPassword,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static void AssertKpi(string html, string label, string value)
    {
        Assert.Matches(
            $"<div class=\"hms-kpi-value\">{Regex.Escape(value)}</div>\\s*<div class=\"hms-kpi-label\">{Regex.Escape(label)}</div>",
            WebUtility.HtmlDecode(html));
    }

    private static Patient NewPatient(string marker, string name) => new()
    {
        PatientCode = $"P-{marker}",
        FullName = $"{name} {marker}",
        Phone = "9000000400",
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

    public sealed class ReceptionistDashboardFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath =
            Path.Combine(Path.GetTempPath(), $"hms-receptionist-dashboard-{Guid.NewGuid():N}.db");

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
