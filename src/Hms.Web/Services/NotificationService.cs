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
    /// <summary>The number of notifications the bell panel shows (acceptance criterion 15.2).</summary>
    public const int RecentTake = 10;

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

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take = RecentTake)
    {
        if (take <= 0)
        {
            return Array.Empty<Notification>();
        }

        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
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

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
    }
}
