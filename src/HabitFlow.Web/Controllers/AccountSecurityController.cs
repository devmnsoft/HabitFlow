using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class AccountSecurityController(RequiredPasswordChangeService service) : Controller
{
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
