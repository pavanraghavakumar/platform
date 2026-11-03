using Hms.Web.Controllers;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Department;
using Hms.Web.ViewModels.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hms.Tests.Admin;

/// <summary>
/// Departments and staff: creating a department, the duplicate-name message, deactivating a
/// department while keeping its doctors, creating a staff member with a sign-in account, and
/// deactivating a staff member so that the account can no longer sign in.
/// Each test migrates its own throwaway SQLite file and checks only the rows it writes.
/// </summary>
public class DepartmentStaffTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public DepartmentStaffTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-department-staff-{Guid.NewGuid():N}.db");
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

        services.AddScoped<IStaffService, StaffService>();

        _services = services.BuildServiceProvider();
    }

    private async Task MigrateAndSeedRolesAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static T WithTempData<T>(T controller) where T : Controller
    {
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return controller;
    }

    [Fact]
    public async Task Create_department_saves_it_active_and_shows_it_in_the_list()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var name = $"Cardiology-{Guid.NewGuid():N}";
        var controller = WithTempData(new DepartmentController(db));

        var result = await controller.Create(new DepartmentCreateViewModel { Name = name });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DepartmentController.Index), redirect.ActionName);

        var saved = await db.Departments.SingleAsync(d => d.Name == name);
        Assert.Equal(EntityStatus.Active, saved.Status);

        var index = Assert.IsType<ViewResult>(await controller.Index(search: name, sort: null, status: null, page: 1));
        var model = Assert.IsType<DepartmentListViewModel>(index.Model);
        Assert.Contains(model.Rows, r => r.Name == name && r.Status == EntityStatus.Active);
    }

    [Fact]
    public async Task Create_department_with_an_existing_name_keeps_the_form_and_shows_the_message()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var name = $"Neurology-{Guid.NewGuid():N}";
        db.Departments.Add(new Department { Name = name, Status = EntityStatus.Active });
        await db.SaveChangesAsync();

        var controller = WithTempData(new DepartmentController(db));
        var result = await controller.Create(new DepartmentCreateViewModel { Name = name });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DepartmentCreateViewModel>(view.Model);
        Assert.Equal(name, model.Name);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState[nameof(DepartmentCreateViewModel.Name)]!.Errors,
            e => e.ErrorMessage == "A department with this name already exists.");
        Assert.Equal(1, await db.Departments.CountAsync(d => d.Name == name));
    }

    [Fact]
    public async Task Deactivate_department_sets_inactive_and_keeps_its_doctors()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var department = new Department { Name = $"Oncology-{Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = "Dr Test",
            Email = $"doctor-{Guid.NewGuid():N}@hms.local",
            DepartmentId = department.Id,
            Status = EntityStatus.Active
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var controller = WithTempData(new DepartmentController(db));
        var result = await controller.Deactivate(department.Id);

        Assert.IsType<RedirectToActionResult>(result);

        var reloaded = await db.Departments.AsNoTracking().SingleAsync(d => d.Id == department.Id);
        Assert.Equal(EntityStatus.Inactive, reloaded.Status);

        var keptDoctor = await db.Doctors.AsNoTracking().SingleAsync(d => d.Id == doctor.Id);
        Assert.Equal(department.Id, keptDoctor.DepartmentId);
    }

    [Fact]
    public async Task Create_staff_saves_active_with_a_sign_in_account_holding_its_role()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var department = new Department { Name = $"General-{Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var email = $"staff-{Guid.NewGuid():N}@hms.local";
        const string password = "Staff1234";
        var controller = WithTempData(new StaffController(db, scope.ServiceProvider.GetRequiredService<IStaffService>()));

        var result = await controller.Create(new StaffCreateViewModel
        {
            FullName = "Test Receptionist",
            Email = email,
            Phone = "9876500000",
            Role = Roles.Receptionist,
            Designation = "Front Desk",
            DepartmentId = department.Id,
            Password = password
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StaffController.Index), redirect.ActionName);

        var saved = await db.Staff.SingleAsync(s => s.Email == email);
        Assert.Equal(EntityStatus.Active, saved.Status);
        Assert.Equal(department.Id, saved.DepartmentId);
        Assert.False(string.IsNullOrEmpty(saved.UserId));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user!.IsActive);
        Assert.True(await users.CheckPasswordAsync(user, password));
        Assert.True(await users.IsInRoleAsync(user, Roles.Receptionist));

        var index = Assert.IsType<ViewResult>(await controller.Index(search: email, sort: null, status: null, page: 1));
        var model = Assert.IsType<StaffListViewModel>(index.Model);
        Assert.Contains(model.Rows, r => r.Email == email && r.Status == EntityStatus.Active);
    }

    [Fact]
    public async Task Create_staff_rejects_an_unrecognized_role_without_creating_an_account()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var department = new Department { Name = $"InvalidRole-{Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var email = $"invalid-role-{Guid.NewGuid():N}@hms.local";
        var controller = WithTempData(new StaffController(db, scope.ServiceProvider.GetRequiredService<IStaffService>()));
        var result = await controller.Create(new StaffCreateViewModel
        {
            FullName = "Invalid Role Staff",
            Email = email,
            Phone = "9876500002",
            Role = "SuperUser",
            Designation = "Invalid",
            DepartmentId = department.Id,
            Password = "Staff1234"
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(view.Model, Assert.IsType<StaffCreateViewModel>(view.Model));
        Assert.Contains(controller.ModelState[nameof(StaffCreateViewModel.Role)]!.Errors,
            error => error.ErrorMessage == "Choose a valid role.");
        Assert.False(await db.Staff.AnyAsync(s => s.Email == email));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync(email));
    }

    [Fact]
    public async Task Create_staff_rolls_back_the_account_when_role_assignment_fails()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var receptionistRole = await roles.FindByNameAsync(Roles.Receptionist);
        Assert.NotNull(receptionistRole);
        var deleted = await roles.DeleteAsync(receptionistRole!);
        Assert.True(deleted.Succeeded);

        var email = $"missing-role-{Guid.NewGuid():N}@hms.local";
        var service = scope.ServiceProvider.GetRequiredService<IStaffService>();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new Staff
            {
                FullName = "Missing Role Staff",
                Email = email,
                Phone = "9876500003",
                Role = Roles.Receptionist,
                Designation = "Front Desk"
            },
            "Staff1234"));

        Assert.NotEmpty(error.Message);
        Assert.False(await db.Staff.AnyAsync(s => s.Email == email));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync(email));
    }

    [Fact]
    public async Task Deactivate_staff_sets_inactive_and_blocks_the_sign_in()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await MigrateAndSeedRolesAsync();

        var email = $"staff-{Guid.NewGuid():N}@hms.local";
        var staffService = scope.ServiceProvider.GetRequiredService<IStaffService>();
        var staff = await staffService.CreateAsync(
            new Staff { FullName = "Test Admin", Email = email, Phone = "9876500001", Role = Roles.Admin, Designation = "Administrator" },
            "Staff1234");

        var controller = WithTempData(new StaffController(db, staffService));
        var result = await controller.Deactivate(staff.Id);
        Assert.IsType<RedirectToActionResult>(result);

        var reloaded = await db.Staff.AsNoTracking().SingleAsync(s => s.Id == staff.Id);
        Assert.Equal(EntityStatus.Inactive, reloaded.Status);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.False(user!.IsActive);
    }

    [Theory]
    [InlineData("Department")]
    [InlineData("Staff")]
    public void List_pages_render_navigation_links_that_keep_the_active_query(string listName)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var viewPath = Path.Combine(repositoryRoot, "src", "Hms.Web", "Views", listName, "Index.cshtml");
        var view = File.ReadAllText(viewPath);

        Assert.Contains("<nav class=\"hms-pagination\"", view);
        Assert.Contains("asp-route-page=\"@(Model.Page - 1)\"", view);
        Assert.Contains("asp-route-page=\"@pageNumber\"", view);
        Assert.Contains("asp-route-page=\"@(Model.Page + 1)\"", view);
        Assert.Contains("asp-route-search=\"@Model.Search\"", view);
        Assert.Contains("asp-route-sort=\"@Model.Sort\"", view);
        Assert.Contains("asp-route-status=\"@Model.Status\"", view);
        Assert.Contains("aria-current=\"page\"", view);
        Assert.Contains("aria-disabled=\"true\"", view);
    }

    [Theory]
    [InlineData("Department")]
    [InlineData("Staff")]
    public void Create_pages_require_confirmation_before_cancel_navigation(string createPageName)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var viewPath = Path.Combine(repositoryRoot, "src", "Hms.Web", "Views", createPageName, "Create.cshtml");
        var view = File.ReadAllText(viewPath);

        Assert.Contains("data-confirm>Cancel</a>", view);
        Assert.Contains("<partial name=\"_ConfirmDialog\"", view);
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

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
