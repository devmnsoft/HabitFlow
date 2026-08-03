using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class NotificationsController(NotificationService notifications, NotificationCenterService center) : Controller
{
    [HttpGet("notifications")]
    public async Task<IActionResult> Index(string filter="all", string? category=null, string? search=null, int page=1, bool archived=false, CancellationToken ct=default) =>
        View(await center.SearchAsync(new(this.CurrentClientId(),this.CurrentUserId(),filter,category,search,page,20,archived),ct));
    [HttpGet("notifications/preview")]
    public async Task<IActionResult> Preview(CancellationToken ct) => PartialView("_NotificationPreview", await center.SearchAsync(new(this.CurrentClientId(),this.CurrentUserId(),"all",PageSize:5),ct));
    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) => Json(new { count = await notifications.CountUnreadAsync(this.CurrentUserId(), ct) });
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await center.SetReadAsync(this.CurrentClientId(), this.CurrentUserId(), id, true, ct); TempData["Success"] = "Notificação marcada como lida."; return RedirectToAction(nameof(Index)); }
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/unread")]
    public async Task<IActionResult> Unread(Guid id,CancellationToken ct){await center.SetReadAsync(this.CurrentClientId(),this.CurrentUserId(),id,false,ct);return RedirectToAction(nameof(Index));}
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id,CancellationToken ct){await center.SetArchivedAsync(this.CurrentClientId(),this.CurrentUserId(),id,true,ct);return RedirectToAction(nameof(Index));}
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id,CancellationToken ct){await center.SetArchivedAsync(this.CurrentClientId(),this.CurrentUserId(),id,false,ct);return RedirectToAction(nameof(Index),new{archived=true});}
    [ValidateAntiForgeryToken, HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { await notifications.MarkAllAsReadAsync(this.CurrentUserId(), ct); TempData["Success"] = "Notificações marcadas como lidas."; return RedirectToAction(nameof(Index)); }
}
