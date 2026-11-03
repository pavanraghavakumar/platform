using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IDoctorService
{
    Task<Doctor> CreateAsync(DoctorCreateViewModel model);
    Task<DoctorEditViewModel?> GetEditAsync(int id);
    Task<bool> UpdateAsync(DoctorEditViewModel model);
    Task<bool> DeactivateAsync(int id);
}

public class DoctorService : IDoctorService
{
    private readonly HmsDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DoctorService(HmsDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<Doctor> CreateAsync(DoctorCreateViewModel model)
    {
        var email = model.Email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.Phone.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        var roleAdded = await _users.AddToRoleAsync(user, Roles.Doctor);
        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", roleAdded.Errors.Select(error => error.Description)));
        }

        var doctor = new Doctor
        {
            FullName = model.FullName.Trim(),
            Email = email,
            Phone = model.Phone.Trim(),
            Specialization = model.Specialization.Trim(),
            DepartmentId = model.DepartmentId,
            Qualifications = model.Qualifications.Trim(),
            YearsOfExperience = model.YearsOfExperience,
            ConsultationFee = model.ConsultationFee,
            Status = EntityStatus.Active,
            UserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return doctor;
    }

    public async Task<DoctorEditViewModel?> GetEditAsync(int id)
    {
        var doctor = await _db.Doctors.AsNoTracking().Include(d => d.Schedules).SingleOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return null;
        }

        return new DoctorEditViewModel
        {
            Id = doctor.Id,
            FullName = doctor.FullName,
            Email = doctor.Email,
            Phone = doctor.Phone,
            Specialization = doctor.Specialization,
            DepartmentId = doctor.DepartmentId,
            Qualifications = doctor.Qualifications ?? string.Empty,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            Schedules = Enum.GetValues<DayOfWeek>().Select(day =>
            {
                var saved = doctor.Schedules.SingleOrDefault(schedule => schedule.DayOfWeek == day);
                return new DoctorScheduleInput
                {
                    DayOfWeek = day,
                    IsWorking = saved?.IsWorking ?? false,
                    StartTime = saved?.StartTime,
                    EndTime = saved?.EndTime
                };
            }).ToList()
        };
    }

    public async Task<bool> UpdateAsync(DoctorEditViewModel model)
    {
        var doctor = await _db.Doctors.Include(d => d.Schedules).SingleOrDefaultAsync(d => d.Id == model.Id);
        if (doctor is null)
        {
            return false;
        }

        var email = model.Email.Trim();
        var duplicate = await _users.FindByEmailAsync(email);
        if (duplicate is not null && duplicate.Id != doctor.UserId)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        doctor.FullName = model.FullName.Trim();
        doctor.Email = email;
        doctor.Phone = model.Phone.Trim();
        doctor.Specialization = model.Specialization.Trim();
        doctor.DepartmentId = model.DepartmentId;
        doctor.Qualifications = model.Qualifications.Trim();
        doctor.YearsOfExperience = model.YearsOfExperience;
        doctor.ConsultationFee = model.ConsultationFee;
        doctor.UpdatedAt = DateTime.UtcNow;

        foreach (var input in model.Schedules)
        {
            var schedule = doctor.Schedules.SingleOrDefault(item => item.DayOfWeek == input.DayOfWeek);
            if (schedule is null)
            {
                schedule = new DoctorSchedule { DoctorId = doctor.Id, DayOfWeek = input.DayOfWeek };
                doctor.Schedules.Add(schedule);
            }

            schedule.IsWorking = input.IsWorking;
            schedule.StartTime = input.IsWorking ? input.StartTime : null;
            schedule.EndTime = input.IsWorking ? input.EndTime : null;
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(doctor.UserId))
        {
            var user = await _users.FindByIdAsync(doctor.UserId);
            if (user is not null)
            {
                user.UserName = email;
                user.Email = email;
                user.FullName = doctor.FullName;
                user.PhoneNumber = doctor.Phone;
                user.UpdatedAt = DateTime.UtcNow;
                var updated = await _users.UpdateAsync(user);
                if (!updated.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(" ", updated.Errors.Select(error => error.Description)));
                }
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var doctor = await _db.Doctors.SingleOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return false;
        }

        doctor.Status = EntityStatus.Inactive;
        doctor.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(doctor.UserId))
        {
            var user = await _users.FindByIdAsync(doctor.UserId);
            if (user is not null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _users.UpdateAsync(user);
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
