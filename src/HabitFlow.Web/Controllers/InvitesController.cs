using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Route("invite")]
public sealed class InvitesController(UserInviteService inviteService) : Controller
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var invite = await inviteService.ValidateTokenAsync(token, ct);
        if (invite is null)
        {
            TempData["Error"] = "Este convite é inválido ou expirou.";
            return View("~/Views/Invites/Accept.cshtml", null);
        }
        ViewBag.Token = token;
        return View("~/Views/Invites/Accept.cshtml", invite);
    }

    [Authorize]
    [HttpPost("{token}/accept")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptPost(string token, CancellationToken ct)
    {
        await inviteService.AcceptAsync(token, this.CurrentUserId(), ct);
        TempData["Success"] = "Você agora faz parte deste cliente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
