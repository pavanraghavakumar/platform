using System.Net;
using Hospital.Application.Features.Departments;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.PublicContent.Departments;
using Hospital.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests.Departments;

public sealed class DepartmentsTests : IClassFixture<HospitalWebApplicationFactory>
{
    private readonly HospitalWebApplicationFactory factory;

    public DepartmentsTests(HospitalWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PublicPagesShowOnlyPublishedContentAndPreselectedRoutes()
    {
        await SeedDepartmentsAsync();
        using var client = factory.CreateClient();

        var index = await client.GetStringAsync("/departments");
        Assert.Contains("Cardiology", index);
        Assert.Contains("/media/departments/cardiology.webp", index);
        Assert.Contains("Comprehensive heart and vascular care.", index);
        Assert.Contains("/departments/cardiology", index);
        Assert.DoesNotContain("Draft Neurology", index);
        Assert.DoesNotContain("Archived Oncology", index);

        var details = await client.GetStringAsync("/departments/cardiology");
        Assert.Contains("Complete cardiac care from diagnosis through recovery.", details);
        Assert.Contains("Dr Asha Rao", details);
        Assert.Contains("/doctors/asha-rao", details);
        Assert.DoesNotContain("Dr Hidden Profile", details);
        Assert.Contains("/doctors?department=cardiology", details);
        Assert.Contains("/appointments/book?department=cardiology", details);
    }

    [Theory]
    [InlineData("missing-department")]
    [InlineData("draft-neurology")]
    [InlineData("archived-oncology")]
    public async Task UnavailableDepartmentReturnsBrandedNotFound(string slug)
    {
        await SeedDepartmentsAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/departments/{slug}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Meridian Hospital", html);
        Assert.Contains("This department is not available", html);
        Assert.Contains("href=\"/departments\"", html);
    }

    [Fact]
    public async Task PublishedUpdateIsVisibleOnTheNextRequest()
    {
        await SeedDepartmentsAsync();
        using var client = factory.CreateClient();
        Assert.Contains("Comprehensive heart and vascular care.", await client.GetStringAsync("/departments"));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var department = await database.Departments.FindAsync(101L);
            Assert.NotNull(department);
            department.Description = "Updated cardiac care from the management boundary.";
            await database.SaveChangesAsync();
        }

        var refreshed = await client.GetStringAsync("/departments");
        Assert.Contains("Updated cardiac care from the management boundary.", refreshed);
        Assert.DoesNotContain("Comprehensive heart and vascular care.", refreshed);
    }

    [Fact]
    public async Task StartupUpgradesAnExistingBaseDatabaseWithPublicDepartmentTables()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"hospital-departments-compatibility-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE "AspNetUsers" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
                        "UserName" TEXT NULL,
                        "NormalizedUserName" TEXT NULL,
                        "Email" TEXT NULL,
                        "NormalizedEmail" TEXT NULL,
                        "EmailConfirmed" INTEGER NOT NULL,
                        "PasswordHash" TEXT NULL,
                        "SecurityStamp" TEXT NULL,
                        "ConcurrencyStamp" TEXT NULL,
                        "PhoneNumber" TEXT NULL,
                        "PhoneNumberConfirmed" INTEGER NOT NULL,
                        "TwoFactorEnabled" INTEGER NOT NULL,
                        "LockoutEnd" TEXT NULL,
                        "LockoutEnabled" INTEGER NOT NULL,
                        "AccessFailedCount" INTEGER NOT NULL,
                        "DisplayName" TEXT NOT NULL
                    );
                    INSERT INTO "AspNetUsers" (
                        "Id", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled",
                        "LockoutEnabled", "AccessFailedCount", "DisplayName")
                    VALUES ('existing-user', 1, 0, 0, 0, 0, 'Existing User');
                    """;
                await command.ExecuteNonQueryAsync();
            }

            using var existingDatabaseFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    builder.UseSetting("ConnectionStrings:HospitalDatabase", $"Data Source={databasePath}");
                });
            using var client = existingDatabaseFactory.CreateClient();

            var response = await client.GetAsync("/departments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using var verification = new SqliteConnection($"Data Source={databasePath}");
            await verification.OpenAsync();
            await using var tableQuery = verification.CreateCommand();
            tableQuery.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('PublicDepartments', 'PublicDepartmentDoctors');";
            Assert.Equal(2L, (long)(await tableQuery.ExecuteScalarAsync())!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
            if (File.Exists(databasePath + "-shm")) File.Delete(databasePath + "-shm");
            if (File.Exists(databasePath + "-wal")) File.Delete(databasePath + "-wal");
        }
    }

    private async Task SeedDepartmentsAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var existingDepartment = await database.Departments.FindAsync(101L);
        if (existingDepartment is not null)
        {
            existingDepartment.Description = "Comprehensive heart and vascular care.";
            existingDepartment.Details = "Complete cardiac care from diagnosis through recovery.";
            existingDepartment.PublicationState = DepartmentPublicationState.Published;
            await database.SaveChangesAsync();
            return;
        }

        database.Departments.AddRange(
            new DepartmentRecord
            {
                Id = 101,
                Name = "Cardiology",
                Slug = "cardiology",
                ImagePath = "/media/departments/cardiology.webp",
                Description = "Comprehensive heart and vascular care.",
                Details = "Complete cardiac care from diagnosis through recovery.",
                PublicationState = DepartmentPublicationState.Published,
                PublishedAt = DateTimeOffset.UtcNow
            },
            new DepartmentRecord
            {
                Id = 102,
                Name = "Draft Neurology",
                Slug = "draft-neurology",
                ImagePath = "/media/departments/neurology.webp",
                Description = "Not public.",
                Details = "Not public.",
                PublicationState = DepartmentPublicationState.Draft
            },
            new DepartmentRecord
            {
                Id = 103,
                Name = "Archived Oncology",
                Slug = "archived-oncology",
                ImagePath = "/media/departments/oncology.webp",
                Description = "No longer public.",
                Details = "No longer public.",
                PublicationState = DepartmentPublicationState.Archived,
                ArchivedAt = DateTimeOffset.UtcNow
            });
        database.DepartmentDoctors.AddRange(
            new DepartmentDoctorRecord
            {
                Id = 201,
                DepartmentId = 101,
                Name = "Dr Asha Rao",
                Slug = "asha-rao",
                PhotoPath = "/media/doctors/asha-rao.webp",
                Qualifications = "MD, DM Cardiology",
                Specialization = "Interventional Cardiology",
                PublicationState = DepartmentPublicationState.Published
            },
            new DepartmentDoctorRecord
            {
                Id = 202,
                DepartmentId = 101,
                Name = "Dr Hidden Profile",
                Slug = "hidden-profile",
                PhotoPath = "/media/doctors/hidden.webp",
                Qualifications = "MD",
                Specialization = "Cardiology",
                PublicationState = DepartmentPublicationState.Draft
            });
        await database.SaveChangesAsync();
    }
}
