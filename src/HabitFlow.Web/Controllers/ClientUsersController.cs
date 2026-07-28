using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin/users")]
public sealed class ClientUsersController(UserInviteService inviteService, CurrentTenantService tenant) : Controller
{
    [HttpGet("")]
    [HttpGet("/account/people")]
    public IActionResult Index() => View("~/Views/Admin/Users/Index.cshtml");

    [HttpGet("invite")]
    [HttpGet("/account/invites")]
    public IActionResult Invite() => View("~/Views/Admin/Users/Invite.cshtml");

    [HttpPost("invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(string email, string role, Guid? clientId, CancellationToken ct)
    {
        var targetClientId = clientId ?? tenant.RequireCurrentClientId();
        var inviteRole = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? UserRole.Admin : UserRole.User;
        var (_, token) = await inviteService.CreateInviteAsync(targetClientId, email, inviteRole, ct);
        TempData["Success"] = "Convite enviado.";
        TempData["InviteLink"] = Url.Action("Accept", "Invites", new { token }, Request.Scheme);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/disable")]
    [ValidateAntiForgeryToken]
    public IActionResult Disable(Guid id, string reason)
    {
        TempData["Success"] = "Usuário desativado para este cliente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/change-role")]
    [ValidateAntiForgeryToken]
    public IActionResult ChangeRole(Guid id, string role, string reason)
    {
        TempData["Success"] = "Perfil alterado para este cliente.";
        return RedirectToAction(nameof(Index));
    }
}
