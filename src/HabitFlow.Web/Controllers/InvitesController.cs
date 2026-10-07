using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Route("invite")]
public sealed class InvitesController(UserInviteService inviteService, AuthService authService) : Controller
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

    [HttpGet("{token}/register")]
    public async Task<IActionResult> Register(string token, CancellationToken ct)
    {
        var invite = await inviteService.ValidateTokenAsync(token, ct);
        if (invite is null)
        {
            TempData["Error"] = "Este convite não está mais disponível.";
            return RedirectToAction(nameof(Accept), new { token });
        }
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Accept), new { token });
        return View("~/Views/Invites/Register.cshtml", new InviteRegisterViewModel(token, MaskEmail(invite.Email)));
    }

    [HttpPost("{token}/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string token, string name, string password, string confirmPassword, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Accept), new { token });
        var invite = await inviteService.ValidateTokenAsync(token, ct);
        if (invite is null)
        {
            TempData["Error"] = "Este convite não está mais disponível.";
            return RedirectToAction(nameof(Accept), new { token });
        }
        var result = await authService.RegisterAsync(new RegisterDto(name, invite.Email, password, confirmPassword), ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.Error.Message;
            return View("~/Views/Invites/Register.cshtml", new InviteRegisterViewModel(token, MaskEmail(invite.Email)));
        }
        TempData["Success"] = "Cadastro criado sem abrir uma nova conta de organização. Entre para aceitar o convite.";
        var returnUrl = Url.Action(nameof(Accept), "Invites", new { token });
        return RedirectToAction("Login", "Auth", new { returnUrl });
    }

    [Authorize]
    [HttpPost("{token}/accept")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptPost(string token, CancellationToken ct)
    {
        try
        {
            await inviteService.AcceptAsync(token, this.CurrentUserId(), ct);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Convite aceito. Entre novamente para acessar a conta.";
            return RedirectToAction("Login", "Auth", new { returnUrl = "/dashboard" });
        }
        catch (Exception exception) when (exception is InvalidOperationException or TenantAccessDeniedException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Accept), new { token });
        }
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "e-mail do convite";
        return $"{email[0]}***{email[at..]}";
    }
}
