using System.Net;
using System.Text.Json;
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

namespace Hms.Tests.Portal;

public class PatientPortalTests : IClassFixture<PatientPortalTests.PatientPortalFactory>
{
    private readonly PatientPortalFactory _factory;

    public PatientPortalTests(PatientPortalFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_shows_patient_scoped_next_appointment_invoice_totals_and_five_newest_notifications()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var patient = await DemoPatientAsync(db);
        var department = await db.Departments.FirstAsync();
        var doctor = new Doctor
        {
            FullName = $"Portal Dashboard Doctor {marker}",
            Email = $"portal.dashboard.{marker}@test.local",
            Phone = "9000000404",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500,
            Status = EntityStatus.Active
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var alreadyPassedToday = NewAppointment(patient.Id, doctor.Id, today, 0, EntityStatus.AppointmentConfirmed);
        var next = NewAppointment(patient.Id, doctor.Id, today.AddDays(2), 9, EntityStatus.AppointmentConfirmed);
        var later = NewAppointment(patient.Id, doctor.Id, today.AddDays(3), 10, EntityStatus.AppointmentPending);
        db.Appointments.AddRange(alreadyPassedToday, next, later);
        db.Invoices.AddRange(
            new Invoice { PatientId = patient.Id, InvoiceNumber = $"INV-{marker}-1", Total = 500, PaidAmount = 100, Status = EntityStatus.InvoicePartiallyPaid },
            new Invoice { PatientId = patient.Id, InvoiceNumber = $"INV-{marker}-2", Total = 300, Status = EntityStatus.InvoiceUnpaid });
        for (var i = 0; i < 6; i++)
        {
            db.Notifications.Add(new NotificationEntity
            {
                UserId = patient.UserId!,
                Message = $"Portal notice {marker} {i}",
                CreatedAt = today.AddMinutes(i),
                UpdatedAt = today.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var dashboardOpenedAt = DateTime.UtcNow;
        var expectedUpcoming = (await db.Appointments
            .Where(a => a.PatientId == patient.Id
                && a.AppointmentDate >= today
                && a.Status != EntityStatus.AppointmentCancelled
                && a.Status != EntityStatus.AppointmentNoShow
                && a.Status != EntityStatus.AppointmentCompleted)
            .Select(a => new { a.AppointmentDate, a.StartTime })
            .ToListAsync())
            .Count(a => a.AppointmentDate.Date.Add(a.StartTime) > dashboardOpenedAt);
        var expectedInvoices = await db.Invoices.CountAsync(i => i.PatientId == patient.Id
            && i.Status != EntityStatus.InvoicePaid
            && i.Status != EntityStatus.InvoiceCancelled);
        var expectedBalance = (await db.Invoices
            .Where(i => i.PatientId == patient.Id
                && i.Status != EntityStatus.InvoicePaid
                && i.Status != EntityStatus.InvoiceCancelled)
            .Select(i => new { i.Total, i.PaidAmount })
            .ToListAsync())
            .Sum(i => Math.Max(0, i.Total - i.PaidAmount));

        var html = await GetPatientPageAsync("/Dashboard/Patient");

        Assert.Contains(doctor.FullName, html);
        Assert.Contains(next.AppointmentDate.ToString("dd MMM yyyy"), html);
        AssertKpi(html, "Upcoming appointments", expectedUpcoming);
        Assert.DoesNotContain(alreadyPassedToday.StartTime.ToString(@"hh\:mm"), html);
        AssertKpi(html, "Unpaid invoices", expectedInvoices);
        Assert.Contains($"{expectedBalance:N2}", html);
        Assert.Contains($"Portal notice {marker} 5", html);
        Assert.DoesNotContain($"Portal notice {marker} 0", html);
    }

    [Fact]
    public async Task Booking_page_lists_only_active_doctors_and_slots_endpoint_returns_free_slots()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var date = NextWorkingDay(DateTime.UtcNow.Date.AddDays(1));
        int activeDoctorId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var department = await db.Departments.FirstAsync();
            var active = new Doctor
            {
                FullName = $"Active Portal Doctor {marker}", Email = $"active.{marker}@test.local", Phone = "9000000401",
                Specialization = "Cardiology", DepartmentId = department.Id, ConsultationFee = 750, Status = EntityStatus.Active
            };
            var inactive = new Doctor
            {
                FullName = $"Inactive Portal Doctor {marker}", Email = $"inactive.{marker}@test.local", Phone = "9000000402",
                Specialization = "Neurology", DepartmentId = department.Id, ConsultationFee = 900, Status = EntityStatus.Inactive
            };
            db.Doctors.AddRange(active, inactive);
            await db.SaveChangesAsync();
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = active.Id, DayOfWeek = date.DayOfWeek, IsWorking = true,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0)
            });
            await db.SaveChangesAsync();
            activeDoctorId = active.Id;
        }

        var client = await SignedInPatientClientAsync();
        var html = await client.GetStringAsync("/Portal/Book");
        var slots = await client.GetStringAsync($"/Portal/Slots?doctorId={activeDoctorId}&date={date:yyyy-MM-dd}");

        Assert.Contains($"Active Portal Doctor {marker}", html);
        Assert.DoesNotContain($"Inactive Portal Doctor {marker}", html);
        Assert.Contains("Cardiology", html);
        Assert.Contains("750.00", html);
        Assert.Contains("09:00", slots);
        Assert.Contains("09:30", slots);
    }

    [Fact]
    public async Task Slots_endpoint_omits_elapsed_times_for_today()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var today = DateTime.UtcNow.Date;
        int doctorId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var department = await db.Departments.FirstAsync();
            var doctor = new Doctor
            {
                FullName = $"Same Day Slot Doctor {marker}", Email = $"same-day.{marker}@test.local", Phone = "9000000404",
                Specialization = "General Medicine", DepartmentId = department.Id, ConsultationFee = 500, Status = EntityStatus.Active
            };
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id, DayOfWeek = today.DayOfWeek, IsWorking = true,
                StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(24)
            });
            await db.SaveChangesAsync();
            doctorId = doctor.Id;
        }

        var requestStartedAt = DateTime.UtcNow;
        var client = await SignedInPatientClientAsync();
        var json = await client.GetStringAsync($"/Portal/Slots?doctorId={doctorId}&date={today:yyyy-MM-dd}");
        using var document = JsonDocument.Parse(json);
        var returnedTimes = document.RootElement.GetProperty("slots")
            .EnumerateArray()
            .Select(slot => TimeSpan.Parse(slot.GetProperty("time").GetString()!))
            .ToList();

        Assert.NotEmpty(returnedTimes);
        Assert.All(returnedTimes, time => Assert.True(today.Add(time) > requestStartedAt));
        Assert.DoesNotContain(TimeSpan.Zero, returnedTimes);
    }

    [Fact]
    public async Task Booking_post_rejects_a_forged_time_that_is_not_a_free_scheduled_slot()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var date = NextWorkingDay(DateTime.UtcNow.Date.AddDays(1));
        int doctorId;
        int patientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var department = await db.Departments.FirstAsync();
            var patient = await DemoPatientAsync(db);
            var doctor = new Doctor
            {
                FullName = $"Forged Slot Doctor {marker}", Email = $"forged.{marker}@test.local", Phone = "9000000403",
                Specialization = "General Medicine", DepartmentId = department.Id, ConsultationFee = 500, Status = EntityStatus.Active
            };
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id, DayOfWeek = date.DayOfWeek, IsWorking = true,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0)
            });
            await db.SaveChangesAsync();
            doctorId = doctor.Id;
            patientId = patient.Id;
        }

        var client = await SignedInPatientClientAsync();
        var bookingPage = await client.GetStringAsync("/Portal/Book");
        var response = await client.PostAsync("/Portal/Book", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["DoctorId"] = doctorId.ToString(),
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["Time"] = "09:15",
            ["__RequestVerificationToken"] = ExtractToken(bookingPage)
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Choose a free time slot.", await response.Content.ReadAsStringAsync());
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await verify.Appointments.AnyAsync(a =>
            a.PatientId == patientId && a.DoctorId == doctorId && a.AppointmentDate == date && a.StartTime == new TimeSpan(9, 15, 0)));
    }

    [Fact]
    public async Task Portal_lists_only_own_final_or_completed_records_and_links_to_printable_pages()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var patient = await DemoPatientAsync(db);
        var doctor = await db.Doctors.FirstAsync();
        var labTest = await db.LaboratoryTests.FirstAsync();
        var ownAppointment = NewAppointment(patient.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(-2), 8, EntityStatus.AppointmentCompleted);
        db.Appointments.Add(ownAppointment);
        await db.SaveChangesAsync();
        var record = new MedicalRecord
        {
            AppointmentId = ownAppointment.Id, PatientId = patient.Id, DoctorId = doctor.Id,
            Diagnosis = $"Own final diagnosis {marker}", Status = EntityStatus.RecordFinal
        };
        var draftAppointment = NewAppointment(patient.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(-1), 9, EntityStatus.AppointmentCompleted);
        db.Appointments.Add(draftAppointment);
        await db.SaveChangesAsync();
        db.MedicalRecords.AddRange(record, new MedicalRecord
        {
            AppointmentId = draftAppointment.Id, PatientId = patient.Id, DoctorId = doctor.Id,
            Diagnosis = $"Private draft {marker}", Status = EntityStatus.RecordDraft
        });
        db.Prescriptions.Add(new Prescription { PatientId = patient.Id, DoctorId = doctor.Id, Status = EntityStatus.PrescriptionActive, Notes = marker });
        db.LabRequests.AddRange(
            new LabRequest { PatientId = patient.Id, DoctorId = doctor.Id, LaboratoryTestId = labTest.Id, Status = EntityStatus.LabCompleted, ResultValue = marker },
            new LabRequest { PatientId = patient.Id, DoctorId = doctor.Id, LaboratoryTestId = labTest.Id, Status = EntityStatus.LabRequested, Remarks = marker });
        db.Invoices.Add(new Invoice { PatientId = patient.Id, InvoiceNumber = $"PORTAL-{marker}", Total = 125, Status = EntityStatus.InvoiceUnpaid });
        await db.SaveChangesAsync();

        var records = await GetPatientPageAsync("/Portal/Records");
        var prescriptions = await GetPatientPageAsync("/Portal/Prescriptions");
        var labs = await GetPatientPageAsync("/Portal/LabReports");
        var invoices = await GetPatientPageAsync("/Portal/Invoices");

        Assert.Contains($"Own final diagnosis {marker}", records);
        Assert.DoesNotContain($"Private draft {marker}", records);
        Assert.Contains("/Prescription/Print/", prescriptions);
        Assert.Contains("/LabRequest/Print/", labs);
        Assert.Contains("/Invoice/Print/", invoices);
        Assert.Contains($"PORTAL-{marker}", invoices);
    }

    [Fact]
    public async Task Patient_can_cancel_only_an_owned_pending_or_confirmed_appointment()
    {
        int ownId;
        int completedId;
        int otherId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var patient = await DemoPatientAsync(db);
            var doctor = await db.Doctors.FirstAsync();
            var other = new Patient { PatientCode = $"P-{Guid.NewGuid():N}", FullName = "Other Portal Patient", Phone = "9000000499", Gender = "Other" };
            db.Patients.Add(other);
            await db.SaveChangesAsync();
            var own = NewAppointment(patient.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(5), 8, EntityStatus.AppointmentPending);
            var completed = NewAppointment(patient.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(-1), 9, EntityStatus.AppointmentCompleted);
            var otherAppointment = NewAppointment(other.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(5), 10, EntityStatus.AppointmentConfirmed);
            db.Appointments.AddRange(own, completed, otherAppointment);
            await db.SaveChangesAsync();
            ownId = own.Id;
            completedId = completed.Id;
            otherId = otherAppointment.Id;
        }

        var client = await SignedInPatientClientAsync();
        var list = await client.GetStringAsync("/Portal/Appointments");
        Assert.Contains($"/Portal/Appointments/{ownId}/Cancel", list);
        Assert.Contains("data-confirm=\"Cancel this appointment?\"", list);
        await PostCancelAsync(client, list, ownId);
        var token = ExtractToken(list);
        await PostCancelWithTokenAsync(client, token, completedId);
        await PostCancelWithTokenAsync(client, token, otherId);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.Equal(EntityStatus.AppointmentCancelled, (await verify.Appointments.FindAsync(ownId))!.Status);
        Assert.Equal(EntityStatus.AppointmentCompleted, (await verify.Appointments.FindAsync(completedId))!.Status);
        Assert.Equal(EntityStatus.AppointmentConfirmed, (await verify.Appointments.FindAsync(otherId))!.Status);
    }

    [Fact]
    public async Task Another_patients_medical_record_redirects_to_access_denied_message()
    {
        int recordId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
            var doctor = await db.Doctors.FirstAsync();
            var other = new Patient { PatientCode = $"P-{Guid.NewGuid():N}", FullName = "Protected Patient", Phone = "9000000488", Gender = "Other" };
            db.Patients.Add(other);
            await db.SaveChangesAsync();
            var appointment = NewAppointment(other.Id, doctor.Id, DateTime.UtcNow.Date.AddDays(-1), 7, EntityStatus.AppointmentCompleted);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            var record = new MedicalRecord { AppointmentId = appointment.Id, PatientId = other.Id, DoctorId = doctor.Id, Diagnosis = "Protected", Status = EntityStatus.RecordFinal };
            db.MedicalRecords.Add(record);
            await db.SaveChangesAsync();
            recordId = record.Id;
        }

        var client = await SignedInPatientClientAsync();
        var response = await client.GetAsync($"/MedicalRecord/Details/{recordId}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/AccessDenied", response.Headers.Location?.OriginalString);
        var denied = await client.GetStringAsync("/Account/AccessDenied");
        Assert.Contains("You do not have access to this page.", denied);
    }

    private async Task<string> GetPatientPageAsync(string path)
    {
        var client = await SignedInPatientClientAsync();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> SignedInPatientClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetStringAsync("/Account/Login");
        var token = ExtractToken(login);
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userName"] = DemoDataSeeder.PatientEmail,
            ["password"] = DemoDataSeeder.PatientPassword,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static async Task PostCancelAsync(HttpClient client, string page, int id)
    {
        var form = Regex.Match(page, $"<form[^>]*action=\"/Portal/Appointments/{id}/Cancel\".*?</form>", RegexOptions.Singleline);
        Assert.True(form.Success, $"Cancellation form for appointment {id} was not rendered.");
        var response = await client.PostAsync($"/Portal/Appointments/{id}/Cancel", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractToken(form.Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task PostCancelWithTokenAsync(HttpClient client, string token, int id)
    {
        var response = await client.PostAsync($"/Portal/Appointments/{id}/Cancel", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static string ExtractToken(string html)
    {
        var input = Regex.Match(html, "<input[^>]*__RequestVerificationToken[^>]*>");
        var value = Regex.Match(input.Value, "value=\"([^\"]+)\"");
        Assert.True(value.Success, "An anti-forgery token was expected.");
        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }

    private static void AssertKpi(string html, string label, int value) =>
        Assert.Matches($"<div class=\"hms-kpi-value\">{value}</div>\\s*<div class=\"hms-kpi-label\">{label}</div>", html);

    private static Task<Patient> DemoPatientAsync(HmsDbContext db) =>
        db.Patients.SingleAsync(p => p.Email == DemoDataSeeder.PatientEmail);

    private static Appointment NewAppointment(int patientId, int doctorId, DateTime date, int hour, string status) => new()
    {
        PatientId = patientId, DoctorId = doctorId, AppointmentDate = date,
        StartTime = new TimeSpan(hour, 0, 0), Status = status
    };

    private static DateTime NextWorkingDay(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Sunday) date = date.AddDays(1);
        return date;
    }

    public sealed class PatientPortalFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"hms-patient-portal-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            SqliteConnection.ClearAllPools();
            if (File.Exists(_databasePath)) File.Delete(_databasePath);
        }
    }
}
