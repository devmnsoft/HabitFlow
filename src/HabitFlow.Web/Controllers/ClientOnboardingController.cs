using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin/onboarding")]
public sealed class ClientOnboardingController(ProductActivationService activation, ClientOnboardingService onboarding, CurrentUserContext currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        return View("~/Views/Admin/Onboarding.cshtml", await activation.ChecklistAsync(currentUser.ClientId.Value, ct));
    }

    [HttpPost("ignore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ignore(string stepCode, string reason, CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        var result = await activation.IgnoreAsync(currentUser.ClientId.Value, currentUser.UserId, stepCode, reason, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Etapa ignorada com a justificativa informada." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("progress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Progress(string stepCode, CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        var result = await activation.MarkInProgressAsync(currentUser.ClientId.Value, currentUser.UserId, stepCode, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Etapa marcada como em andamento." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("finish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        var page = await activation.ChecklistAsync(currentUser.ClientId.Value, ct);
        if (!page.CanFinish)
        {
            TempData["Error"] = "Conclua, bloqueie pelo plano ou ignore com justificativa cada etapa antes de ativar.";
            return RedirectToAction(nameof(Index));
        }
        await onboarding.FinishAsync(currentUser.ClientId.Value, ct);
        TempData["Success"] = "Implantação concluída. Sua conta está pronta para uso.";
        return RedirectToAction("Index", "Dashboard");
    }
}
