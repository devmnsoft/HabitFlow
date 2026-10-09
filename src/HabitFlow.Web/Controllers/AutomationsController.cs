using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("automations")]
public sealed class AutomationsController(HabitAutomationService automations, IUserFacingErrorMapper errors) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new AutomationsIndexViewModel(await automations.ListAsync(this.CurrentClientId(), this.CurrentUserId(), ct)));

    [HttpPost("create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HabitAutomationType type, Guid? habitId, string? frequency, CancellationToken ct)
    {
        var result = await automations.CreateAsync(this.CurrentClientId(), this.CurrentUserId(), type, habitId, frequency ?? "Weekly", ct);
        TempData[result.IsFailure ? "Warning" : "Success"] = result.IsFailure
            ? errors.ToPublicMessage(result.Error.Code, "automations")
            : "Automacao criada com quiet hours e controle do usuario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/pause"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        await automations.PauseAsync(id, this.CurrentClientId(), this.CurrentUserId(), ct);
        TempData["Success"] = "Automacao pausada.";
        return RedirectToAction(nameof(Index));
    }
}
