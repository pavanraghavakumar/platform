using Hms.Web.Controllers;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hms.Tests.Admin;

public class DoctorManagementTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public DoctorManagementTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-doctor-management-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(options =>
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
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Create_saves_active_doctor_and_doctor_sign_in_account()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var department = await AddDepartmentAsync(db, "Cardiology");
        var email = $"doctor-{Guid.NewGuid():N}@hms.local";
        var controller = CreateController(scope);

        var result = await controller.Create(new DoctorCreateViewModel
        {
            FullName = "Dr Meera Shah",
            Email = email,
            Phone = "9876500010",
            Specialization = "Cardiologist",
            DepartmentId = department.Id,
            Qualifications = "MD Cardiology",
            YearsOfExperience = 12,
            ConsultationFee = 850m,
            Password = "Doctor123"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var doctor = await db.Doctors.SingleAsync(item => item.Email == email);
        Assert.Equal(EntityStatus.Active, doctor.Status);
        Assert.Equal("MD Cardiology", doctor.Qualifications);
        Assert.Equal(850m, doctor.ConsultationFee);
        Assert.False(string.IsNullOrWhiteSpace(doctor.UserId));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user!.IsActive);
        Assert.True(await users.CheckPasswordAsync(user, "Doctor123"));
        Assert.True(await users.IsInRoleAsync(user, Roles.Doctor));
    }

    [Fact]
    public async Task Index_filters_by_department_and_returns_required_columns()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var cardiology = await AddDepartmentAsync(db, "Cardiology");
        var neurology = await AddDepartmentAsync(db, "Neurology");
        db.Doctors.AddRange(
            DoctorFor(cardiology.Id, "Dr Cardio", "Cardiologist", 700m),
            DoctorFor(neurology.Id, "Dr Neuro", "Neurologist", 900m));
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await CreateController(scope).Index(cardiology.Id));
        var model = Assert.IsType<DoctorListViewModel>(view.Model);

        var row = Assert.Single(model.Rows);
        Assert.Equal("Dr Cardio", row.FullName);
        Assert.Equal("Cardiologist", row.Specialization);
        Assert.Equal(cardiology.Name, row.DepartmentName);
        Assert.Equal(700m, row.ConsultationFee);
        Assert.Equal(EntityStatus.Active, row.Status);
    }

    [Fact]
    public async Task Edit_saves_all_seven_schedule_days_and_new_fee()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var department = await AddDepartmentAsync(db, "General Medicine");
        var doctor = DoctorFor(department.Id, "Dr Schedule", "Physician", 500m);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();
        var model = EditModel(doctor, 725m);
        model.Schedules = Enum.GetValues<DayOfWeek>().Select(day => new DoctorScheduleInput
        {
            DayOfWeek = day,
            IsWorking = day is not DayOfWeek.Sunday,
            StartTime = day is not DayOfWeek.Sunday ? new TimeSpan(9, 0, 0) : null,
            EndTime = day is not DayOfWeek.Sunday ? new TimeSpan(17, 0, 0) : null
        }).ToList();

        var result = await CreateController(scope).Edit(doctor.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = await db.Doctors.AsNoTracking().SingleAsync(item => item.Id == doctor.Id);
        Assert.Equal(725m, saved.ConsultationFee);
        var schedules = await db.DoctorSchedules.AsNoTracking().Where(item => item.DoctorId == doctor.Id).ToListAsync();
        Assert.Equal(7, schedules.Count);
        Assert.False(schedules.Single(item => item.DayOfWeek == DayOfWeek.Sunday).IsWorking);
        Assert.All(schedules.Where(item => item.DayOfWeek != DayOfWeek.Sunday), item =>
        {
            Assert.True(item.IsWorking);
            Assert.Equal(new TimeSpan(9, 0, 0), item.StartTime);
            Assert.Equal(new TimeSpan(17, 0, 0), item.EndTime);
        });
    }

    [Fact]
    public async Task Edit_with_end_not_after_start_keeps_form_open_with_exact_message()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var department = await AddDepartmentAsync(db, "Orthopaedics");
        var doctor = DoctorFor(department.Id, "Dr Invalid Hours", "Orthopaedic", 600m);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();
        var model = EditModel(doctor, 600m);
        model.Schedules.Add(new DoctorScheduleInput
        {
            DayOfWeek = DayOfWeek.Monday,
            IsWorking = true,
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(9, 0, 0)
        });

        var controller = CreateController(scope);
        var view = Assert.IsType<ViewResult>(await controller.Edit(doctor.Id, model));

        Assert.Same(model, view.Model);
        Assert.Contains(controller.ModelState["Schedules[0].EndTime"]!.Errors,
            error => error.ErrorMessage == "End time must be after start time.");
        Assert.Empty(await db.DoctorSchedules.Where(item => item.DoctorId == doctor.Id).ToListAsync());
    }

    [Fact]
    public async Task Details_shows_schedule_and_appointment_history_newest_first()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var department = await AddDepartmentAsync(db, "Paediatrics");
        var doctor = DoctorFor(department.Id, "Dr Profile", "Paediatrician", 550m);
        var patient = new Patient { PatientCode = $"P-{Guid.NewGuid():N}"[..7], FullName = "Test Patient", Phone = "9000000000" };
        db.AddRange(doctor, patient);
        await db.SaveChangesAsync();
        db.DoctorSchedules.Add(new DoctorSchedule { DoctorId = doctor.Id, DayOfWeek = DayOfWeek.Tuesday, IsWorking = true, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0) });
        db.Appointments.AddRange(
            new Appointment { DoctorId = doctor.Id, PatientId = patient.Id, AppointmentDate = new DateTime(2026, 4, 1), StartTime = new TimeSpan(10, 0, 0) },
            new Appointment { DoctorId = doctor.Id, PatientId = patient.Id, AppointmentDate = new DateTime(2026, 4, 2), StartTime = new TimeSpan(11, 0, 0) });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await CreateController(scope).Details(doctor.Id));
        var model = Assert.IsType<DoctorDetailsViewModel>(view.Model);

        Assert.Equal("Dr Profile", model.FullName);
        Assert.Single(model.Schedules);
        Assert.Equal(new DateTime(2026, 4, 2), model.Appointments[0].AppointmentDate);
        Assert.Equal(new DateTime(2026, 4, 1), model.Appointments[1].AppointmentDate);
    }

    [Fact]
    public async Task Deactivate_sets_doctor_and_account_inactive_and_blocks_new_booking()
    {
        using var scope = await CreateScopeAsync();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var department = await AddDepartmentAsync(db, "Dermatology");
        var service = scope.ServiceProvider.GetRequiredService<IDoctorService>();
        var doctor = await service.CreateAsync(new DoctorCreateViewModel
        {
            FullName = "Dr Inactive",
            Email = $"inactive-{Guid.NewGuid():N}@hms.local",
            Phone = "9876500020",
            Specialization = "Dermatologist",
            DepartmentId = department.Id,
            Qualifications = "MD",
            YearsOfExperience = 5,
            ConsultationFee = 450m,
            Password = "Doctor123"
        });
        var patient = new Patient { PatientCode = $"P-{Guid.NewGuid():N}"[..7], FullName = "Booking Patient", Phone = "9000000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        await CreateController(scope).Deactivate(doctor.Id);

        var saved = await db.Doctors.AsNoTracking().SingleAsync(item => item.Id == doctor.Id);
        Assert.Equal(EntityStatus.Inactive, saved.Status);
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(doctor.UserId!);
        Assert.NotNull(user);
        Assert.False(user!.IsActive);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IAppointmentService>().BookAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1), new TimeSpan(9, 0, 0), null));
        Assert.Equal("This doctor is not available for booking.", error.Message);
        Assert.False(await db.Appointments.AnyAsync(item => item.DoctorId == doctor.Id && item.PatientId == patient.Id));
    }

    private async Task<IServiceScope> CreateScopeAsync()
    {
        var scope = _services.CreateScope();
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
        return scope;
    }

    private DoctorController CreateController(IServiceScope scope)
    {
        var controller = new DoctorController(
            scope.ServiceProvider.GetRequiredService<HmsDbContext>(),
            scope.ServiceProvider.GetRequiredService<IDoctorService>());
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return controller;
    }

    private static async Task<Department> AddDepartmentAsync(HmsDbContext db, string prefix)
    {
        var department = new Department { Name = $"{prefix}-{Guid.NewGuid():N}", Status = EntityStatus.Active };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        return department;
    }

    private static Doctor DoctorFor(int departmentId, string name, string specialization, decimal fee) => new()
    {
        FullName = name,
        Email = $"{Guid.NewGuid():N}@hms.local",
        Phone = "9876500000",
        Specialization = specialization,
        DepartmentId = departmentId,
        Qualifications = "MD",
        YearsOfExperience = 5,
        ConsultationFee = fee,
        Status = EntityStatus.Active
    };

    private static DoctorEditViewModel EditModel(Doctor doctor, decimal fee) => new()
    {
        Id = doctor.Id,
        FullName = doctor.FullName,
        Email = doctor.Email,
        Phone = doctor.Phone,
        Specialization = doctor.Specialization,
        DepartmentId = doctor.DepartmentId,
        Qualifications = doctor.Qualifications ?? "MD",
        YearsOfExperience = doctor.YearsOfExperience,
        ConsultationFee = fee
    };

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
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
