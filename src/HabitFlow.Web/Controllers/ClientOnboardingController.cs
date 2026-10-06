using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin/onboarding")]
public sealed class ClientOnboardingController(
    ClientOnboardingService onboarding,
    CurrentUserContext currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();

        var state = await onboarding.GetOrCreateAsync(currentUser.ClientId.Value, ct);
        return View("~/Views/Admin/Onboarding.cshtml", ClientOnboardingService.BuildChecklist(state));
    }

    [HttpPost("finish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();

        await onboarding.FinishAsync(currentUser.ClientId.Value, ct);
        TempData["Success"] = "Implantação concluída. Sua conta está pronta para uso.";
        return RedirectToAction("Index", "Dashboard");
    }
}
