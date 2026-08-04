using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class AccountSecurityController(RequiredPasswordChangeService service, UserSessionService sessionService, SessionRevocationService revocationService, IUserRepository users) : Controller
{
    [HttpGet("/account/security")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await users.GetByIdAsync(this.CurrentUserId(), ct);
        if (user is null) return Challenge();
        var current = Guid.TryParse(User.FindFirst("session_id")?.Value, out var id) ? id : (Guid?)null;
        return View(new AccountSecurityViewModel(user.Email, null, await sessionService.ListAsync(user.Id, user.ClientId, current, ct), current));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/account/security/sessions/revoke")]
    public async Task<IActionResult> Revoke(RevokeSessionViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        var result = await revocationService.RevokeAsync(this.CurrentUserId(), this.CurrentClientIdOrNull(), model.SessionId, model.Password, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Sessão encerrada com segurança." : result.Error.Message;
        if (result.IsSuccess && User.FindFirst("session_id")?.Value == model.SessionId.ToString()) await HttpContext.SignOutAsync();
        return result.IsSuccess && User.FindFirst("session_id")?.Value == model.SessionId.ToString() ? RedirectToAction("Login", "Auth") : RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/account/security/sessions/revoke-all")]
    public async Task<IActionResult> RevokeAll(RevokeAllSessionsViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        var result = await revocationService.RevokeAllAsync(this.CurrentUserId(), model.Password, ct);
        if (result.IsFailure) { TempData["Error"] = result.Error.Message; return RedirectToAction(nameof(Index)); }
        await HttpContext.SignOutAsync();
        TempData["Success"] = "Todas as sessões foram encerradas.";
        return RedirectToAction("Login", "Auth");
    }
    [HttpGet("/account/security/change-required-password")]
    public IActionResult ChangeRequiredPassword() =>
        User.HasClaim("must_change_password", "true") ? View(new RequiredPasswordChangeViewModel()) : RedirectToAction("Index", "Dashboard");

    [ValidateAntiForgeryToken]
    [HttpPost("/account/security/change-required-password")]
    public async Task<IActionResult> ChangeRequiredPassword(RequiredPasswordChangeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.ChangeAsync(this.CurrentUserId(), model.CurrentPassword, model.NewPassword, model.Confirmation, ct);
        if (result.IsFailure) { ModelState.AddModelError(string.Empty, result.Error.Message); return View(model); }
        await HttpContext.SignOutAsync();
        TempData["Success"] = "Senha atualizada e sessões anteriores encerradas. Entre novamente para continuar.";
        return RedirectToAction("Login", "Auth");
    }
}
