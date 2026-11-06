using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.PublicContent.MedicalServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Infrastructure.Data;

public static class DatabaseInitializer
{
    private static readonly (string Role, string Email, string Password, string Name)[] DevelopmentAccounts =
    {
        ("Patient", "patient@example.test", "Greenfield-Patient-2026!", "Demo Patient"),
        ("Admin", "admin@example.test", "Greenfield-Admin-2026!", "Demo Administrator"),
        ("Doctor", "doctor@example.test", "Greenfield-Doctor-2026!", "Demo Doctor"),
        ("Receptionist", "receptionist@example.test", "Greenfield-Reception-2026!", "Demo Receptionist")
    };

    public static async Task InitializeAsync(IServiceProvider services, bool isProduction, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

        EnsureDatabaseDirectoryExists(database);

        if ((await database.Database.GetPendingMigrationsAsync()).Any())
        {
            await database.Database.MigrateAsync();
        }
        else
        {
            await database.Database.EnsureCreatedAsync();
        }

        await EnsurePublicDepartmentSchemaAsync(database);
        await MedicalServicePublicContentInitializer.InitializeAsync(database, !isProduction);
        if (!isProduction)
        {
            await SeedDevelopmentAccountsIfEmptyAsync(scope.ServiceProvider, database);
        }
        else
        {
            await SeedFirstProductionAdminIfEmptyAsync(scope.ServiceProvider, database, configuration);
        }
    }

    private static async Task EnsurePublicDepartmentSchemaAsync(HospitalDbContext database)
    {
        await database.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "PublicDepartments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PublicDepartments" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Slug" TEXT NOT NULL,
                "ImagePath" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "Details" TEXT NOT NULL,
                "PublicationState" TEXT NOT NULL,
                "PublishedAt" TEXT NULL,
                "ArchivedAt" TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PublicDepartments_Slug"
                ON "PublicDepartments" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_PublicDepartments_PublicationState"
                ON "PublicDepartments" ("PublicationState");

            CREATE TABLE IF NOT EXISTS "PublicDepartmentDoctors" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PublicDepartmentDoctors" PRIMARY KEY AUTOINCREMENT,
                "DepartmentId" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "Slug" TEXT NOT NULL,
                "PhotoPath" TEXT NOT NULL,
                "Qualifications" TEXT NOT NULL,
                "Specialization" TEXT NOT NULL,
                "PublicationState" TEXT NOT NULL,
                CONSTRAINT "FK_PublicDepartmentDoctors_PublicDepartments_DepartmentId"
                    FOREIGN KEY ("DepartmentId") REFERENCES "PublicDepartments" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PublicDepartmentDoctors_Slug"
                ON "PublicDepartmentDoctors" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_PublicDepartmentDoctors_DepartmentId_PublicationState"
                ON "PublicDepartmentDoctors" ("DepartmentId", "PublicationState");
            """);
    }

    private static void EnsureDatabaseDirectoryExists(HospitalDbContext database)
    {
        var dataSource = database.Database.GetDbConnection().DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static async Task SeedDevelopmentAccountsIfEmptyAsync(IServiceProvider services, HospitalDbContext database)
    {
        if (await database.Users.AnyAsync())
        {
            return;
        }

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var account in DevelopmentAccounts)
        {
            if (!await roles.RoleExistsAsync(account.Role))
            {
                EnsureSucceeded(await roles.CreateAsync(new IdentityRole(account.Role)));
            }

            var user = new ApplicationUser
            {
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true,
                DisplayName = account.Name
            };
            EnsureSucceeded(await users.CreateAsync(user, account.Password));
            EnsureSucceeded(await users.AddToRoleAsync(user, account.Role));
        }
    }

    private static async Task SeedFirstProductionAdminIfEmptyAsync(
        IServiceProvider services,
        HospitalDbContext database,
        IConfiguration configuration)
    {
        if (await database.Users.AnyAsync())
        {
            return;
        }

        var email = configuration["FIRST_ADMIN_EMAIL"];
        var password = configuration["FIRST_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roles.RoleExistsAsync("Admin"))
        {
            EnsureSucceeded(await roles.CreateAsync(new IdentityRole("Admin")));
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Administrator" };
        EnsureSucceeded(await users.CreateAsync(user, password));
        EnsureSucceeded(await users.AddToRoleAsync(user, "Admin"));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
