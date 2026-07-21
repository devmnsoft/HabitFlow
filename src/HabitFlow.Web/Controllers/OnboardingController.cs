using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class OnboardingController(GuidedJourneyService journey, HabitLibraryService library, IHabitObjectiveRepository objectives, AuditService audit) : Controller
{
    [HttpGet("/onboarding")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var options = await journey.GetStartOptionsAsync(ct);
        return View(options.Value ?? Array.Empty<HabitObjective>());
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/onboarding/select-objective")]
    public async Task<IActionResult> SelectObjective(string slug, CancellationToken ct)
    {
        await audit.LogAsync("onboarding_objective_selected", "Objetivo selecionado no onboarding", userId: this.CurrentUserId(), email: User.Identity?.Name, metadata: new { slug }, ct: ct);
        return RedirectToAction(nameof(Templates), new { slug });
    }

    [HttpGet("/onboarding/templates/{slug}")]
    public async Task<IActionResult> Templates(string slug, CancellationToken ct)
    {
        var objective = await objectives.GetBySlugAsync(slug, ct);
        if (objective is null) return NotFound();
        var templates = await library.GetTemplatesByObjectiveAsync(slug, ct);
        return View((objective, Templates: templates.Value ?? Array.Empty<HabitTemplate>()));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/onboarding/add-template")]
    public async Task<IActionResult> AddTemplate(Guid templateId, CancellationToken ct)
    {
        var result = await journey.CompleteFirstHabitFromTemplateAsync(this.CurrentUserSnapshot(), templateId, ct);
        if (result.IsFailure) { TempData["Error"] = result.Error.Message; return RedirectToAction(nameof(Index)); }
        TempData["Success"] = "Pronto. Seu primeiro hábito foi criado.";
        return RedirectToAction(nameof(Complete));
    }

    [HttpGet("/onboarding/complete")]
    public IActionResult Complete() => View();

    [ValidateAntiForgeryToken]
    [HttpPost("/onboarding/complete")]
    public IActionResult CompletePost() => RedirectToAction("Index", "Dashboard");
}
