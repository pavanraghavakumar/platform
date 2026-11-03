using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Web.Controllers;

/// <summary>
/// The notification bell of the top navigation bar: the unread count it shows and the
/// panel it opens. Notifications are rows in the database; nothing is emailed or texted.
/// </summary>
[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(INotificationService notifications, UserManager<ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    /// <summary>
    /// The bell panel: the signed-in user's 10 most recent notifications, newest first,
    /// and opening it marks that user's notifications as read (criteria 15.2 and 15.3).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Panel()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return PartialView("_NotificationPanel", Array.Empty<Notification>());
        }

        var notifications = await _notifications.GetRecentAsync(user.Id, NotificationService.RecentTake);
        await _notifications.MarkAllReadAsync(user.Id);
        return PartialView("_NotificationPanel", notifications);
    }

    /// <summary>The unread count the bell icon shows: <c>{ "count": 3 }</c> (criterion 15.1).</summary>
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        var count = user is null ? 0 : await _notifications.GetUnreadCountAsync(user.Id);
        return Json(new { count });
    }
}
