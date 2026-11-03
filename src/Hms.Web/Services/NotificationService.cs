using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface INotificationService
{
    Task AddAsync(string userId, string message);

    Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take = 10);

    Task<int> GetUnreadCountAsync(string userId);

    Task MarkAllReadAsync(string userId);
}

/// <summary>
/// The one way a notification is created. Notifications are rows in the database
/// shown inside the app; no email and no SMS is ever sent.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly HmsDbContext _db;

    public NotificationService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(string userId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take = 10)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId)
    {
        return _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
