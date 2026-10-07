using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin/users")]
public sealed class ClientUsersController(
    UserInviteService inviteService,
    AccountPeopleService peopleService,
    AccountCapacityService capacityService,
    CurrentTenantService tenant) : Controller
{
    [HttpGet("")]
    [HttpGet("/account/people")]
    public async Task<IActionResult> Index(string? search, string? role, string? status, int currentPage = 1, int pageSize = 20, Guid? clientId = null, CancellationToken ct = default)
    {
        try
        {
            var model = await peopleService.SearchAsync(ResolveClientId(clientId), search, role, status, currentPage, pageSize, ct);
            return View("~/Views/Admin/Users/Index.cshtml", model);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpGet("invite")]
    public IActionResult Invite() => View("~/Views/Admin/Users/Invite.cshtml");

    [HttpGet("invites")]
    [HttpGet("/account/invites")]
    public async Task<IActionResult> Invites(Guid? clientId = null, CancellationToken ct = default)
    {
        var targetClientId = ResolveClientId(clientId);
        try
        {
            var invites = await inviteService.GetSummariesByClientAsync(targetClientId, ct);
            return View("~/Views/Admin/Users/Invites.cshtml",
                new AccountInvitesViewModel(invites, tenant.IsSuperAdmin() ? targetClientId : null,
                    await capacityService.GetUsageAsync(targetClientId, ct)));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(string email, string role, Guid? clientId, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<UserRole>(role, true, out var inviteRole) || inviteRole is not (UserRole.Admin or UserRole.User))
                throw new ArgumentException("Selecione um perfil válido.");
            var (_, token) = await inviteService.CreateInviteAsync(ResolveClientId(clientId), email, inviteRole, ct);
            TempData["Success"] = "Convite criado. Compartilhe este link com a pessoa convidada.";
            TempData["InviteLink"] = Url.Action("Accept", "Invites", new { token }, Request.Scheme);
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException or ArgumentException)
        {
            TempData["InviteEmail"] = email;
            TempData["InviteRole"] = role;
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Invites), clientId is null ? null : new { clientId });
    }

    [HttpPost("invites/{id:guid}/resend")]
    [HttpPost("/account/invites/{id:guid}/resend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(Guid id, Guid? clientId, CancellationToken ct)
    {
        try
        {
            var (_, token) = await inviteService.RotateTokenAsync(id, ct);
            TempData["Success"] = "O link foi rotacionado; o link anterior deixou de ser válido.";
            TempData["InviteLink"] = Url.Action("Accept", "Invites", new { token }, Request.Scheme);
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Invites), clientId is null ? null : new { clientId });
    }

    [HttpPost("invites/{id:guid}/cancel")]
    [HttpPost("/account/invites/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, Guid? clientId, CancellationToken ct)
    {
        try
        {
            if (await inviteService.CancelAsync(id, ct)) TempData["Success"] = "Convite cancelado.";
            else TempData["Error"] = "Este convite já não está pendente; nenhum vínculo existente foi removido.";
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Invites), clientId is null ? null : new { clientId });
    }

    [HttpPost("{id:guid}/disable")]
    [HttpPost("/account/people/{id:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(Guid id, string reason, string? search, string? role, string? status, int currentPage = 1, Guid? clientId = null, CancellationToken ct = default)
    {
        try
        {
            await peopleService.SetActiveAsync(ResolveClientId(clientId), id, false, reason, ct);
            TempData["Success"] = "A pessoa foi desativada; o histórico foi preservado e as sessões foram revogadas.";
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException or ArgumentException)
        {
            PreserveFailedAction(id, "disable", reason);
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { search, role, status, currentPage, clientId });
    }

    [HttpPost("{id:guid}/enable")]
    [HttpPost("/account/people/{id:guid}/enable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(Guid id, string reason, string? search, string? role, string? status, int currentPage = 1, Guid? clientId = null, CancellationToken ct = default)
    {
        try
        {
            await peopleService.SetActiveAsync(ResolveClientId(clientId), id, true, reason, ct);
            TempData["Success"] = "A pessoa foi reativada.";
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException or ArgumentException)
        {
            PreserveFailedAction(id, "enable", reason);
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { search, role, status, currentPage, clientId });
    }

    [HttpPost("{id:guid}/change-role")]
    [HttpPost("/account/people/{id:guid}/change-role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid id, string role, string reason, string? search, string? filterRole, string? status, int currentPage = 1, Guid? clientId = null, CancellationToken ct = default)
    {
        try
        {
            if (!Enum.TryParse<UserRole>(role, true, out var newRole))
                throw new ArgumentException("Selecione um perfil válido.");
            await peopleService.ChangeRoleAsync(ResolveClientId(clientId), id, newRole, reason, ct);
            TempData["Success"] = "O perfil foi atualizado.";
        }
        catch (Exception ex) when (ex is TenantAccessDeniedException or InvalidOperationException or ArgumentException)
        {
            PreserveFailedAction(id, "change-role", reason, role);
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { search, role = filterRole, status, currentPage, clientId });
    }

    private Guid ResolveClientId(Guid? clientId) => clientId is not null && tenant.IsSuperAdmin()
        ? clientId.Value
        : tenant.RequireCurrentClientId();

    private void PreserveFailedAction(Guid userId, string action, string reason, string? role = null)
    {
        TempData["FailedUserId"] = userId.ToString();
        TempData["FailedAction"] = action;
        TempData["FailedReason"] = reason;
        if (role is not null) TempData["FailedRole"] = role;
    }
}
