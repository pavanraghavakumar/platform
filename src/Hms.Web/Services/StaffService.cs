using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

/// <summary>
/// Creates and deactivates staff members together with their sign-in account, so the
/// staff record and the account that signs in never drift apart.
/// </summary>
public interface IStaffService
{
    /// <summary>
    /// Saves the staff member with status Active and creates a sign-in account that holds
    /// the staff member's role. Returns the saved staff member.
    /// </summary>
    Task<Staff> CreateAsync(Staff staff, string password);

    /// <summary>
    /// Sets the staff member's status to Inactive and blocks the linked account's sign-in.
    /// Returns false when no staff member has that id.
    /// </summary>
    Task<bool> DeactivateAsync(int id);
}

public class StaffService : IStaffService
{
    private static readonly string[] AllowedRoles = { Roles.Receptionist, Roles.Admin };

    private readonly HmsDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public StaffService(HmsDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<Staff> CreateAsync(Staff staff, string password)
    {
        var email = (staff.Email ?? string.Empty).Trim();
        var role = string.IsNullOrWhiteSpace(staff.Role) ? Roles.Receptionist : staff.Role;
        if (!AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Choose a valid role.");
        }

        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = staff.FullName,
            PhoneNumber = staff.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        var roleAdded = await _users.AddToRoleAsync(user, role);
        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", roleAdded.Errors.Select(e => e.Description)));
        }

        staff.Email = email;
        staff.Status = EntityStatus.Active;
        staff.UserId = user.Id;
        staff.CreatedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;

        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return staff;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var staff = await _db.Staff.FirstOrDefaultAsync(s => s.Id == id);
        if (staff is null)
        {
            return false;
        }

        staff.Status = EntityStatus.Inactive;
        staff.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(staff.UserId))
        {
            var user = await _users.FindByIdAsync(staff.UserId);
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
