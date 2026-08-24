using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class NotificationsController(NotificationService notifications, NotificationCenterService center,
    PushSubscriptionService subscriptions, PushNotificationPreferenceService preferences,
    PushNotificationService push, Microsoft.Extensions.Options.IOptions<PushNotificationOptions> pushOptions) : Controller
{
    [HttpGet("notifications")]
    public async Task<IActionResult> Index(string filter="all", string? category=null, string? search=null, int page=1, bool archived=false, CancellationToken ct=default) =>
        View(await center.SearchAsync(new(this.CurrentClientId(),this.CurrentUserId(),filter,category,search,page,20,archived),ct));
    [HttpGet("notifications/preview")]
    public async Task<IActionResult> Preview(CancellationToken ct) => PartialView("_NotificationPreview", await center.SearchAsync(new(this.CurrentClientId(),this.CurrentUserId(),"all",PageSize:5),ct));
    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) => Json(new { count = await notifications.CountUnreadAsync(this.CurrentUserId(), ct) });
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { SetActionFeedback(await center.SetReadAsync(this.CurrentClientId(), this.CurrentUserId(), id, true, ct), "Notificação marcada como lida."); return RedirectToAction(nameof(Index)); }
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/unread")]
    public async Task<IActionResult> Unread(Guid id,CancellationToken ct){SetActionFeedback(await center.SetReadAsync(this.CurrentClientId(),this.CurrentUserId(),id,false,ct),"Notificação marcada como não lida.");return RedirectToAction(nameof(Index));}
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id,CancellationToken ct){SetActionFeedback(await center.SetArchivedAsync(this.CurrentClientId(),this.CurrentUserId(),id,true,ct),"Notificação arquivada.");return RedirectToAction(nameof(Index));}
    [ValidateAntiForgeryToken, HttpPost("notifications/{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id,CancellationToken ct){SetActionFeedback(await center.SetArchivedAsync(this.CurrentClientId(),this.CurrentUserId(),id,false,ct),"Notificação restaurada.");return RedirectToAction(nameof(Index),new{archived=true});}
    [ValidateAntiForgeryToken, HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { var count=await center.MarkAllAsReadAsync(this.CurrentClientId(),this.CurrentUserId(),ct); TempData[count>0?"Success":"Info"] = count>0?$"{count} notificação(ões) marcada(s) como lida(s).":"Você já está em dia com as notificações."; return RedirectToAction(nameof(Index)); }
    [ValidateAntiForgeryToken, HttpPost("notifications/archive-read")]
    public async Task<IActionResult> ArchiveRead(CancellationToken ct) { var count=await center.ArchiveReadAsync(this.CurrentClientId(),this.CurrentUserId(),ct); TempData[count>0?"Success":"Info"] = count>0?$"{count} notificação(ões) lida(s) arquivada(s).":"Não há notificações lidas para arquivar."; return RedirectToAction(nameof(Index)); }

    [HttpGet("notifications/preferences")]
    public async Task<IActionResult> Preferences(CancellationToken ct) => View(new Models.PushNotificationViewModel(
        pushOptions.Value.PublicKey, await preferences.GetAsync(this.CurrentClientId(), this.CurrentUserId(), ct),
        await subscriptions.ListAsync(this.CurrentClientId(), this.CurrentUserId(), ct)));

    [ValidateAntiForgeryToken, HttpPost("notifications/push/subscriptions")]
    public async Task<IActionResult> Subscribe([FromBody] Models.PushSubscriptionRequest request, CancellationToken ct)
    { await subscriptions.SubscribeAsync(this.CurrentClientId(),this.CurrentUserId(),request.Endpoint,request.P256Dh,request.Auth,request.DeviceName??"Este dispositivo",ct); return Ok(new{status="subscribed"}); }

    [ValidateAntiForgeryToken, HttpDelete("notifications/push/subscriptions/{id:guid}")]
    public async Task<IActionResult> RemoveDevice(Guid id,CancellationToken ct) => await subscriptions.RemoveAsync(this.CurrentClientId(),this.CurrentUserId(),id,ct) ? NoContent() : NotFound();

    [ValidateAntiForgeryToken, HttpPost("notifications/preferences")]
    public async Task<IActionResult> SavePreferences(bool pushEnabled,bool internalEnabled,TimeOnly? quietStart,TimeOnly? quietEnd,int maximumPerDay,DateTime? pausedUntil,CancellationToken ct)
    { await preferences.SaveAsync(new(this.CurrentClientId(),this.CurrentUserId(),pushEnabled,internalEnabled,quietStart,quietEnd,maximumPerDay,pausedUntil),ct); TempData["Success"]="Preferências atualizadas."; return RedirectToAction(nameof(Preferences)); }

    [ValidateAntiForgeryToken, HttpPost("notifications/push/test")]
    public async Task<IActionResult> TestPush(CancellationToken ct) { var count=await push.SendSafeReminderAsync(this.CurrentClientId(),this.CurrentUserId(),ct); TempData[count>0?"Success":"Info"]=count>0?"Notificação de teste enviada.":"Nenhum dispositivo ativo recebeu o teste."; return RedirectToAction(nameof(Preferences)); }

    private void SetActionFeedback(bool changed,string success){TempData[changed?"Success":"Error"]=changed?success:"Essa notificação não existe ou não está disponível para sua conta.";}
}
