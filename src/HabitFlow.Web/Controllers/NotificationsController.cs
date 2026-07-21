using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class NotificationsController(NotificationService notifications) : Controller
{
    [HttpGet("notifications")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await notifications.ListUnreadAsync(this.CurrentUserId(), ct));
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await notifications.MarkAsReadAsync(this.CurrentUserId(), id, ct); return RedirectToAction(nameof(Index)); }
    [ValidateAntiForgeryToken, HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { await notifications.MarkAllAsReadAsync(this.CurrentUserId(), ct); return RedirectToAction(nameof(Index)); }
}
