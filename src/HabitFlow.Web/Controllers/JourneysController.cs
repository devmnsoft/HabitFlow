using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("journeys")]
public sealed class JourneysController(HabitJourneyService journeys, IUserFacingErrorMapper errors) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new JourneysIndexViewModel(await journeys.ListAsync(this.CurrentClientId(), ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var details = await journeys.DetailsAsync(id, this.CurrentClientId(), this.CurrentUserId(), ct);
        return details is null ? NotFound() : View(new JourneyDetailsViewModel(details));
    }

    [HttpPost("{id:guid}/join"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(Guid id, bool confirmed, CancellationToken ct)
    {
        var result = await journeys.JoinAsync(new JoinHabitJourneyCommand(this.CurrentClientId(), this.CurrentUserId(), id, confirmed, HttpContext.TraceIdentifier), ct);
        if (result.IsFailure) TempData["Warning"] = errors.ToPublicMessage(result.Error.Code, "journeys");
        else TempData["Success"] = "Jornada ativada. Revise os habitos sugeridos antes de cria-los.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/leave"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        await journeys.LeaveAsync(id, this.CurrentClientId(), this.CurrentUserId(), HttpContext.TraceIdentifier, ct);
        TempData["Success"] = "Voce saiu da jornada. Seu historico foi preservado.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
