using Hms.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notifications;
    private readonly UserManager<Models.Entities.ApplicationUser> _userManager;

    public NotificationController(INotificationService notifications, UserManager<Models.Entities.ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IActionResult> Panel()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return PartialView("_NotificationPanel", Array.Empty<Models.Entities.Notification>());
        }

        var notifications = await _notifications.GetRecentAsync(user.Id, 10);
        await _notifications.MarkAllReadAsync(user.Id);
        return PartialView("_NotificationPanel", notifications);
    }

    public async Task<IActionResult> UnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        var count = user is null ? 0 : await _notifications.GetUnreadCountAsync(user.Id);
        return Json(new { count });
    }
}
