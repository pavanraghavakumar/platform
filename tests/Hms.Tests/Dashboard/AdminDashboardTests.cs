using System.Net;
using System.Net.Http.Headers;
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

using AppointmentEntity = Hms.Web.Models.Entities.Appointment;

namespace Hms.Tests.Dashboard;

/// <summary>
/// The Admin dashboard at <c>/Dashboard/Admin</c>: KPI figures, charts, recent activity and the
/// department list, each read from the database. Every test writes its own rows into a throwaway
/// SQLite file and checks only those rows.
/// </summary>
public class AdminDashboardTests : IClassFixture<AdminDashboardTests.DashboardFactory>
{
    private readonly DashboardFactory _factory;

    public AdminDashboardTests(DashboardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_dashboard_shows_the_entity_counts_from_the_database()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Cardiology {marker}" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = $"Dr Marker {marker}",
            Email = $"doctor.{marker}@hms.local",
            Phone = "9000000001",
            Specialization = "Cardiology",
            DepartmentId = department.Id,
            ConsultationFee = 500m
        };
        db.Doctors.Add(doctor);
        db.Staff.Add(new Staff
        {
            FullName = $"Staff Marker {marker}",
            Email = $"staff.{marker}@hms.local",
            Phone = "9000000002",
            Role = Roles.Receptionist,
            Designation = "Front Desk",
            DepartmentId = department.Id
        });
        db.Patients.Add(new Patient
        {
            PatientCode = $"P-{marker}",
            FullName = $"Patient Marker {marker}",
            Phone = "9000000003",
            Gender = "Female"
        });
        await db.SaveChangesAsync();

        var expected = new
        {
            Patients = await db.Patients.CountAsync(),
            Doctors = await db.Doctors.CountAsync(),
            Staff = await db.Staff.CountAsync(),
            Departments = await db.Departments.CountAsync()
        };

        var html = await GetAdminDashboardAsync();

        Assert.Contains("Total patients", html);
        Assert.Contains("Total doctors", html);
        Assert.Contains("Total staff", html);
        Assert.Contains("Departments", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expected.Patients}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expected.Doctors}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expected.Staff}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expected.Departments}</div>", html);
    }

    [Fact]
    public async Task Admin_dashboard_shows_todays_appointments_pending_completed_revenue_and_outstanding()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"General {marker}" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = $"Dr Money {marker}",
            Email = $"money.{marker}@hms.local",
            Phone = "9000000011",
            Specialization = "General",
            DepartmentId = department.Id,
            ConsultationFee = 400m
        };
        var patient = new Patient
        {
            PatientCode = $"P-M{marker}",
            FullName = $"Patient Money {marker}",
            Phone = "9000000012",
            Gender = "Male"
        };
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var appointment = new AppointmentEntity
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = today,
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentPending
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        db.MedicalRecords.Add(new MedicalRecord
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Marker diagnosis",
            Status = EntityStatus.RecordFinal
        });

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{marker}",
            PatientId = patient.Id,
            Total = 1000m,
            PaidAmount = 250m,
            Status = EntityStatus.InvoicePartiallyPaid
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.Payments.Add(new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 250m,
            Method = "Cash",
            PaidAt = DateTime.UtcNow,
            Status = EntityStatus.PaymentRecorded
        });
        await db.SaveChangesAsync();

        var expectedToday = await db.Appointments
            .CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1));
        var expectedPending = await db.Appointments
            .CountAsync(a => a.Status == EntityStatus.AppointmentPending);
        var expectedCompleted = await db.MedicalRecords
            .CountAsync(r => r.Status == EntityStatus.RecordFinal);
        var expectedRevenue = (await db.Payments
            .Where(p => p.Status == EntityStatus.PaymentRecorded)
            .Select(p => p.Amount)
            .ToListAsync()).Sum();
        var expectedOutstanding = (await db.Invoices
            .Where(i => i.Status != EntityStatus.InvoiceCancelled)
            .Select(i => new { i.Total, i.PaidAmount })
            .ToListAsync()).Sum(i => i.Total - i.PaidAmount);

        var html = await GetAdminDashboardAsync();

        Assert.Contains("Today&#x27;s appointments", html);
        Assert.Contains("Pending appointments", html);
        Assert.Contains("Completed consultations", html);
        Assert.Contains("Total revenue", html);
        Assert.Contains("Outstanding balance", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expectedToday}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expectedPending}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">{expectedCompleted}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">&#x20B9;{expectedRevenue:N2}</div>", html);
        Assert.Contains($"<div class=\"hms-kpi-value\">&#x20B9;{expectedOutstanding:N2}</div>", html);
    }

    [Fact]
    public async Task Admin_dashboard_draws_the_revenue_and_appointment_charts_from_the_database()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Chart {marker}" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = $"Dr Chart {marker}",
            Email = $"chart.{marker}@hms.local",
            Phone = "9000000021",
            Specialization = "General",
            DepartmentId = department.Id,
            ConsultationFee = 300m
        };
        var patient = new Patient
        {
            PatientCode = $"P-C{marker}",
            FullName = $"Patient Chart {marker}",
            Phone = "9000000022",
            Gender = "Female"
        };
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        db.Appointments.Add(new AppointmentEntity
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = today,
            StartTime = new TimeSpan(11, 0, 0),
            Status = EntityStatus.AppointmentConfirmed
        });

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-C{marker}",
            PatientId = patient.Id,
            Total = 750m,
            PaidAmount = 750m,
            Status = EntityStatus.InvoicePaid
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.Payments.Add(new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 750m,
            Method = "UPI",
            PaidAt = DateTime.UtcNow,
            Status = EntityStatus.PaymentRecorded
        });
        await db.SaveChangesAsync();

        var html = await GetAdminDashboardAsync();

        Assert.Contains("id=\"hmsRevenueChart\"", html);
        Assert.Contains("id=\"hmsAppointmentChart\"", html);
        Assert.Contains("/js/pages/dashboard.js", html);
        Assert.Contains("/lib/chart.js/chart.umd.min.js", html);

        var revenueLabels = ChartAttribute(html, "hmsRevenueChart", "data-labels");
        Assert.Equal(6, revenueLabels.Split('|').Length);
        Assert.Contains(DateTime.UtcNow.ToString("MMM yyyy"), revenueLabels);

        var revenueValues = ChartAttribute(html, "hmsRevenueChart", "data-values");
        Assert.Equal(6, revenueValues.Split('|').Length);

        var trendLabels = ChartAttribute(html, "hmsAppointmentChart", "data-labels");
        Assert.Equal(7, trendLabels.Split('|').Length);

        var trendValues = ChartAttribute(html, "hmsAppointmentChart", "data-values");
        Assert.Equal(7, trendValues.Split('|').Length);
        Assert.Contains(today.ToString("ddd dd MMM"), trendLabels);
    }

    [Fact]
    public async Task Admin_dashboard_lists_the_five_most_recent_patients_and_appointments_newest_first()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Recent {marker}" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = $"Dr Recent {marker}",
            Email = $"recent.{marker}@hms.local",
            Phone = "9000000031",
            Specialization = "General",
            DepartmentId = department.Id,
            ConsultationFee = 300m
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var baseTime = DateTime.UtcNow.AddMinutes(-30);
        for (var i = 0; i < 6; i++)
        {
            db.Patients.Add(new Patient
            {
                PatientCode = $"P-R{marker}-{i}",
                FullName = $"Recent Patient {marker} {i}",
                Phone = $"90000001{i:00}",
                Gender = "Male",
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var patients = await db.Patients
            .Where(p => p.PatientCode.StartsWith($"P-R{marker}-"))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        for (var i = 0; i < 6; i++)
        {
            db.Appointments.Add(new AppointmentEntity
            {
                PatientId = patients[i].Id,
                DoctorId = doctor.Id,
                AppointmentDate = today.AddDays(-i),
                StartTime = new TimeSpan(9, 0, 0),
                Status = EntityStatus.AppointmentConfirmed
            });
        }
        await db.SaveChangesAsync();

        var html = await GetAdminDashboardAsync();

        Assert.Contains("Recent patient registrations", html);
        Assert.Contains("Recent appointments", html);

        // The newest patient is shown, the oldest of the six is not.
        Assert.Contains($"Recent Patient {marker} 5", html);
        Assert.DoesNotContain($"Recent Patient {marker} 0", html);

        // Newest first: the newest patient appears before the second newest.
        Assert.True(
            html.IndexOf($"Recent Patient {marker} 5", StringComparison.Ordinal)
            < html.IndexOf($"Recent Patient {marker} 4", StringComparison.Ordinal),
            "Recent patients should be newest first.");

        // The newest appointment (today) is shown, the oldest (five days ago) is not.
        Assert.Contains(today.ToString("dd MMM yyyy"), html);
        Assert.DoesNotContain(today.AddDays(-5).ToString("dd MMM yyyy"), html);
    }

    [Fact]
    public async Task Admin_dashboard_lists_each_department_with_its_doctor_count()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Neurology {marker}" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        for (var i = 0; i < 2; i++)
        {
            db.Doctors.Add(new Doctor
            {
                FullName = $"Dr Neuro {marker} {i}",
                Email = $"neuro.{marker}.{i}@hms.local",
                Phone = $"90000002{i:00}",
                Specialization = "Neurology",
                DepartmentId = department.Id,
                ConsultationFee = 600m
            });
        }
        await db.SaveChangesAsync();

        var html = await GetAdminDashboardAsync();

        var row = Regex.Match(
            html,
            $"<tr>\\s*<td>Neurology {marker}</td>\\s*<td>.*?</td>\\s*<td>(\\d+)</td>\\s*</tr>",
            RegexOptions.Singleline);

        Assert.True(row.Success, "The department list should show the department with its doctor count.");
        Assert.Equal("2", row.Groups[1].Value);
    }

    [Fact]
    public async Task Admin_dashboard_is_not_shown_to_a_doctor()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await SignInAsync(client, DemoDataSeeder.DoctorEmail, DemoDataSeeder.DoctorPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Dashboard/Admin");
        request.Headers.Add("Cookie", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    private async Task<string> GetAdminDashboardAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await SignInAsync(client, DemoDataSeeder.AdminEmail, DemoDataSeeder.AdminPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Dashboard/Admin");
        request.Headers.Add("Cookie", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string ChartAttribute(string html, string canvasId, string attribute)
    {
        var match = Regex.Match(
            html,
            $"id=\"{canvasId}\"[^>]*{attribute}=\"([^\"]*)\"",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"The {canvasId} canvas should carry {attribute}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<string> SignInAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetStringAsync("/Account/Login");
        var tokenMatch = Regex.Match(
            loginPage,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success, "The sign-in page should carry an anti-forgery token.");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userName"] = email,
            ["password"] = password,
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
        });

        var response = await client.PostAsync("/Account/Login", content);
        Assert.True(
            response.Headers.TryGetValues("Set-Cookie", out var cookies),
            "Signing in should set the authentication cookie.");

        var authCookie = cookies!
            .Select(c => c.Split(';')[0])
            .FirstOrDefault(c => c.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));
        Assert.NotNull(authCookie);
        return authCookie!;
    }

    /// <summary>
    /// Boots the app on a throwaway SQLite file in the temporary folder, migrated and seeded once
    /// for every test of this class.
    /// </summary>
    public sealed class DashboardFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath =
            Path.Combine(Path.GetTempPath(), $"hms-admin-dashboard-{Guid.NewGuid():N}.db");

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
