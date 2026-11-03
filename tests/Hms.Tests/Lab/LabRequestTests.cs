using System.Net;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests.Lab;

/// <summary>
/// Laboratory test catalog and lab requests (Requirements 8.1 to 8.8). Each test boots the real
/// app on its own throwaway SQLite file in the temp folder, seeds the four roles itself and
/// asserts only on the rows it writes.
/// </summary>
public class LabRequestTests : IAsyncLifetime
{
    private const string AdminEmail = "lab.admin@hms.local";
    private const string AdminPassword = "Admin@123";
    private const string DoctorEmail = "lab.doctor@hms.local";
    private const string DoctorPassword = "Doctor@123";
    private const string OtherDoctorEmail = "lab.other@hms.local";
    private const string OtherDoctorPassword = "Doctor@123";
    private const string ReceptionEmail = "lab.reception@hms.local";
    private const string ReceptionPassword = "Reception@123";
    private const string PatientEmail = "lab.patient@hms.local";
    private const string PatientPassword = "Patient@123";

    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private WebApplicationFactory<Program> _factory = null!;

    public LabRequestTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-lab-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();
    }

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<HmsDbContext>)
                                || d.ServiceType == typeof(HmsDbContext)
                                || d.ServiceType == typeof(SqliteConnection))
                    .ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
            });
        });

        // Booting the app migrates the database and seeds the demo records.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();
        await SeedLabUsersAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Fact]
    public async Task Admin_adds_a_laboratory_test_to_the_catalog()
    {
        using var client = await SignInAsync(AdminEmail, AdminPassword);

        var token = await GetAntiForgeryTokenAsync(client, "/LabTest/Create");
        var response = await client.PostAsync("/LabTest/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Serum Creatinine",
            ["Category"] = "Biochemistry",
            ["Price"] = "275.50",
            ["NormalRange"] = "0.6-1.2 mg/dL"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var saved = await db.LaboratoryTests.SingleAsync(t => t.Name == "Serum Creatinine");
        Assert.Equal("Biochemistry", saved.Category);
        Assert.Equal(275.50m, saved.Price);
        Assert.Equal("0.6-1.2 mg/dL", saved.NormalRange);
    }

    [Fact]
    public async Task Doctor_can_reach_the_request_form_from_the_consultation_page()
    {
        var (patientId, _, appointmentId, _) = await SeedConsultationAsync();
        using var client = await SignInAsync(DoctorEmail, DoctorPassword);

        var html = await client.GetStringAsync($"/Consultation/Index?appointmentId={appointmentId}");

        Assert.Contains("Request laboratory tests", html);
        Assert.Contains($"/LabRequest/Create?patientId={patientId}&amp;appointmentId={appointmentId}", html);
    }

    [Fact]
    public async Task Doctor_requests_tests_creating_one_request_per_test_linked_to_patient_doctor_and_appointment()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        using var client = await SignInAsync(DoctorEmail, DoctorPassword);

        var token = await GetAntiForgeryTokenAsync(client, $"/LabRequest/Create?patientId={patientId}&appointmentId={appointmentId}");
        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("patientId", patientId.ToString()),
            new("appointmentId", appointmentId.ToString())
        };
        foreach (var testId in testIds)
        {
            form.Add(new("testIds", testId.ToString()));
        }

        var response = await client.PostAsync("/LabRequest/Create", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var created = await db.LabRequests
            .Where(r => r.AppointmentId == appointmentId)
            .ToListAsync();

        Assert.Equal(testIds.Length, created.Count);
        Assert.All(created, r =>
        {
            Assert.Equal(EntityStatus.LabRequested, r.Status);
            Assert.Equal(patientId, r.PatientId);
            Assert.Equal(doctorId, r.DoctorId);
            Assert.Equal(appointmentId, r.AppointmentId);
        });
        Assert.Equal(testIds.OrderBy(x => x), created.Select(r => r.LaboratoryTestId).OrderBy(x => x));
        Assert.Equal($"/LabRequest/Details/{created[0].Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Doctor_cannot_create_requests_with_patient_or_appointment_identifiers_from_another_consultation()
    {
        var (patientId, _, appointmentId, testIds) = await SeedConsultationAsync();
        var (otherPatientId, otherAppointmentId) = await SeedOtherDoctorsConsultationAsync();
        using var client = await SignInAsync(DoctorEmail, DoctorPassword);

        var token = await GetAntiForgeryTokenAsync(client, $"/LabRequest/Create?patientId={patientId}&appointmentId={appointmentId}");
        var wrongPatientResponse = await client.PostAsync("/LabRequest/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["patientId"] = otherPatientId.ToString(),
            ["appointmentId"] = appointmentId.ToString(),
            ["testIds"] = testIds[0].ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, wrongPatientResponse.StatusCode);
        Assert.Contains("/Account/AccessDenied", wrongPatientResponse.Headers.Location!.ToString());

        token = await GetAntiForgeryTokenAsync(client, $"/LabRequest/Create?patientId={patientId}&appointmentId={appointmentId}");
        var wrongAppointmentResponse = await client.PostAsync("/LabRequest/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["patientId"] = otherPatientId.ToString(),
            ["appointmentId"] = otherAppointmentId.ToString(),
            ["testIds"] = testIds[0].ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, wrongAppointmentResponse.StatusCode);
        Assert.Contains("/Account/AccessDenied", wrongAppointmentResponse.Headers.Location!.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        Assert.False(await db.LabRequests.AnyAsync(r =>
            r.AppointmentId == appointmentId && r.LaboratoryTestId == testIds[0]));
        Assert.False(await db.LabRequests.AnyAsync(r =>
            r.AppointmentId == otherAppointmentId && r.LaboratoryTestId == testIds[0]));
    }

    [Fact]
    public async Task Receptionist_list_shows_requests_and_filters_by_status()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestedId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabRequested);
        var completedId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[1], EntityStatus.LabCompleted);

        using var client = await SignInAsync(ReceptionEmail, ReceptionPassword);

        var all = await client.GetStringAsync("/LabRequest/Index");
        Assert.Contains("Lab requests", all);
        Assert.Contains("Requested", all);
        Assert.Contains("Completed", all);

        var filtered = await client.GetStringAsync("/LabRequest/Index?status=Completed");
        Assert.Contains($"LabRequest/Details/{completedId}", filtered);
        Assert.DoesNotContain($"LabRequest/Details/{requestedId}", filtered);
    }

    [Fact]
    public async Task Receptionist_starts_a_requested_lab_request()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabRequested);

        using var client = await SignInAsync(ReceptionEmail, ReceptionPassword);
        var token = await GetAntiForgeryTokenAsync(client, "/LabRequest/Index");
        var response = await client.PostAsync($"/LabRequest/Start/{requestId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var request = await db.LabRequests.SingleAsync(r => r.Id == requestId);
        Assert.Equal(EntityStatus.LabInProgress, request.Status);
    }

    [Fact]
    public async Task Receptionist_completes_an_in_progress_request_and_notifies_the_doctor()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabInProgress);

        using var client = await SignInAsync(ReceptionEmail, ReceptionPassword);
        var token = await GetAntiForgeryTokenAsync(client, $"/LabRequest/Complete/{requestId}");
        var response = await client.PostAsync($"/LabRequest/Complete/{requestId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["resultValue"] = "5.4 x10^9/L",
            ["remarks"] = "Within normal range"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var request = await db.LabRequests.SingleAsync(r => r.Id == requestId);
        Assert.Equal(EntityStatus.LabCompleted, request.Status);
        Assert.Equal("5.4 x10^9/L", request.ResultValue);
        Assert.Equal("Within normal range", request.Remarks);
        Assert.NotNull(request.ResultEnteredAt);

        var doctor = await db.Doctors.SingleAsync(d => d.Id == doctorId);
        var notifications = await db.Notifications
            .Where(n => n.UserId == doctor.UserId)
            .ToListAsync();
        Assert.NotEmpty(notifications);
    }

    [Fact]
    public async Task Requesting_doctor_sees_the_result_of_a_completed_request()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabCompleted);

        using var client = await SignInAsync(DoctorEmail, DoctorPassword);
        var html = await client.GetStringAsync($"/LabRequest/Details/{requestId}");

        Assert.Contains("Complete Blood Count", html);
        Assert.Contains("4.5-11.0 x10^9/L", html);
        Assert.Contains("5.4 x10^9/L", html);
        Assert.Contains("Within normal range", html);
    }

    [Fact]
    public async Task Another_doctor_cannot_open_a_request_that_doctor_did_not_request()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabCompleted);

        using var client = await SignInAsync(OtherDoctorEmail, OtherDoctorPassword);
        var response = await client.GetAsync($"/LabRequest/Details/{requestId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Printable_report_shows_the_hospital_patient_test_and_result()
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], EntityStatus.LabCompleted);

        using var client = await SignInAsync(DoctorEmail, DoctorPassword);
        var html = await client.GetStringAsync($"/LabRequest/Print/{requestId}");

        Assert.Contains("Greenfield General Hospital", html);
        Assert.Contains("Lab Patient", html);
        Assert.Contains("P-", html);
        Assert.Contains("Complete Blood Count", html);
        Assert.Contains("4.5-11.0 x10^9/L", html);
        Assert.Contains("5.4 x10^9/L", html);
        Assert.Contains("Within normal range", html);
        Assert.Contains("Print", html);
    }

    [Theory]
    [InlineData(EntityStatus.LabRequested)]
    [InlineData(EntityStatus.LabInProgress)]
    public async Task Patient_cannot_open_owned_request_before_it_is_completed(string status)
    {
        var (patientId, doctorId, appointmentId, testIds) = await SeedConsultationAsync();
        var requestId = await CreateRequestAsync(patientId, doctorId, appointmentId, testIds[0], status);
        using var client = await SignInAsync(PatientEmail, PatientPassword);

        foreach (var action in new[] { "Details", "Print" })
        {
            var response = await client.GetAsync($"/LabRequest/{action}/{requestId}");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
        }
    }

    private async Task<(int PatientId, int DoctorId, int AppointmentId, int[] TestIds)> SeedConsultationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Lab Dept {Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctorUser = await db.Users.SingleAsync(u => u.Email == DoctorEmail);
        var doctor = new Doctor
        {
            FullName = "Dr. Lab Requesting",
            Email = DoctorEmail,
            Phone = "9000000001",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            UserId = doctorUser.Id
        };
        db.Doctors.Add(doctor);

        var patientUser = await db.Users.SingleAsync(u => u.Email == PatientEmail);
        var patient = new Patient
        {
            PatientCode = $"P-LAB{Guid.NewGuid():N}".Substring(0, 12),
            FullName = "Lab Patient",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            Phone = "9000000002",
            Status = EntityStatus.Active,
            UserId = patientUser.Id
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentInProgress
        };
        db.Appointments.Add(appointment);

        var test1 = new LaboratoryTest { Name = "Complete Blood Count", Category = "Haematology", Price = 350m, NormalRange = "4.5-11.0 x10^9/L" };
        var test2 = new LaboratoryTest { Name = "Blood Sugar (Fasting)", Category = "Biochemistry", Price = 200m, NormalRange = "70-100 mg/dL" };
        db.LaboratoryTests.AddRange(test1, test2);
        await db.SaveChangesAsync();

        return (patient.Id, doctor.Id, appointment.Id, new[] { test1.Id, test2.Id });
    }

    private async Task<int> CreateRequestAsync(int patientId, int doctorId, int appointmentId, int testId, string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var request = new LabRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = appointmentId,
            LaboratoryTestId = testId,
            Status = status,
            ResultValue = status == EntityStatus.LabCompleted ? "5.4 x10^9/L" : null,
            Remarks = status == EntityStatus.LabCompleted ? "Within normal range" : null,
            ResultEnteredAt = status == EntityStatus.LabCompleted ? DateTime.UtcNow : null
        };
        db.LabRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }

    private async Task<(int PatientId, int AppointmentId)> SeedOtherDoctorsConsultationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();

        var department = new Department { Name = $"Other Lab Dept {Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctorUser = await db.Users.SingleAsync(u => u.Email == OtherDoctorEmail);
        var doctor = new Doctor
        {
            FullName = "Dr. Other Consultation",
            Email = OtherDoctorEmail,
            Phone = "9000000003",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            UserId = doctorUser.Id
        };
        db.Doctors.Add(doctor);

        var patient = new Patient
        {
            PatientCode = $"P-OTH{Guid.NewGuid():N}".Substring(0, 12),
            FullName = "Other Lab Patient",
            DateOfBirth = new DateTime(1992, 2, 2),
            Gender = "Female",
            Phone = "9000000004",
            Status = EntityStatus.Active
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(11, 0, 0),
            Status = EntityStatus.AppointmentInProgress
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return (patient.Id, appointment.Id);
    }

    private async Task SeedLabUsersAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        await CreateUserAsync(users, AdminEmail, AdminPassword, "Lab Admin", Roles.Admin);
        await CreateUserAsync(users, DoctorEmail, DoctorPassword, "Dr. Lab Requesting", Roles.Doctor);
        await CreateUserAsync(users, OtherDoctorEmail, OtherDoctorPassword, "Dr. Lab Other", Roles.Doctor);
        await CreateUserAsync(users, ReceptionEmail, ReceptionPassword, "Lab Reception", Roles.Receptionist);
        await CreateUserAsync(users, PatientEmail, PatientPassword, "Lab Patient", Roles.Patient);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> users, string email, string password, string fullName, string role)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

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
    }

    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["userName"] = email,
            ["password"] = password
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, $"No anti-forgery token found on {path}.");
        return match.Groups[1].Value;
    }
}
