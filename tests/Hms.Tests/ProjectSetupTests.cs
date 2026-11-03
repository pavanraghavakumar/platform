using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hms.Tests;

/// <summary>
/// Project setup: migrates a throwaway SQLite file in a temporary folder and checks that the
/// demo seeder creates the four demo logins, and that a second run leaves an existing database alone.
/// </summary>
public class ProjectSetupTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public ProjectSetupTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-project-setup-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HmsDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddScoped<DemoDataSeeder>();

        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeder_creates_the_four_demo_logins_on_an_empty_database()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var expected = new (string Email, string Password, string Role)[]
        {
            (DemoDataSeeder.AdminEmail, DemoDataSeeder.AdminPassword, Roles.Admin),
            (DemoDataSeeder.DoctorEmail, DemoDataSeeder.DoctorPassword, Roles.Doctor),
            (DemoDataSeeder.ReceptionistEmail, DemoDataSeeder.ReceptionistPassword, Roles.Receptionist),
            (DemoDataSeeder.PatientEmail, DemoDataSeeder.PatientPassword, Roles.Patient)
        };

        foreach (var (email, password, role) in expected)
        {
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(await users.CheckPasswordAsync(user!, password), $"{email} should sign in with its demo password.");
            Assert.True(await users.IsInRoleAsync(user!, role), $"{email} should hold the {role} role.");
            Assert.True(await roles.RoleExistsAsync(role));
        }
    }

    [Fact]
    public async Task Seeder_creates_the_demo_records_on_an_empty_database()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();

        Assert.True(await db.Departments.CountAsync() >= 4);
        Assert.True(await db.Doctors.CountAsync() >= 4);
        Assert.True(await db.Staff.CountAsync() >= 3);
        Assert.True(await db.Patients.CountAsync() >= 10);
        Assert.True(await db.Appointments.CountAsync() >= 12);
        Assert.True(await db.MedicalRecords.CountAsync() >= 6);
        Assert.True(await db.Prescriptions.CountAsync() >= 5);
        Assert.True(await db.LabRequests.CountAsync() >= 6);
        Assert.True(await db.Medicines.CountAsync() >= 10);
        Assert.True(await db.Invoices.CountAsync() >= 8);
        Assert.True(await db.Payments.CountAsync() >= 8);

        var statuses = await db.Appointments.Select(a => a.Status).Distinct().ToListAsync();
        Assert.Equal(7, statuses.Count);
    }

    [Fact]
    public async Task Seeder_keeps_an_existing_database_unchanged()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();

        var patientsBefore = await db.Patients.CountAsync();
        var departmentsBefore = await db.Departments.CountAsync();

        await seeder.SeedAsync();

        Assert.Equal(patientsBefore, await db.Patients.CountAsync());
        Assert.Equal(departmentsBefore, await db.Departments.CountAsync());
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

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Hms.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
