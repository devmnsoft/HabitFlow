using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class OnboardingController(GuidedJourneyService journey, PersonalOnboardingJourneyService personalJourney, HabitLibraryService library, IHabitObjectiveRepository objectives, AuditService audit, ILogger<OnboardingController> logger) : Controller
{
    [HttpGet("/onboarding")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var progress = await personalJourney.ResumeAsync(this.CurrentClientId(), this.CurrentUserId(), ct);
            if (progress is null || progress.Status == OnboardingStatus.Skipped)
                progress = await personalJourney.StartAsync(this.CurrentClientId(), this.CurrentUserId(), ct);
            ViewData["OnboardingVersion"] = progress.Version;
            ViewData["ResumeStep"] = progress.CurrentStep.ToString();
        }
        catch (Exception ex) { logger.LogWarning(ex, "Progresso detalhado do onboarding indisponível; exibindo fluxo básico"); }
        var options = await journey.GetStartOptionsAsync(ct);
        return View(options.Value ?? Array.Empty<HabitObjective>());
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/onboarding/select-objective")]
    public async Task<IActionResult> SelectObjective(string slug, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        try
        {
            var progress = await personalJourney.ResumeAsync(clientId, userId, ct) ?? await personalJourney.StartAsync(clientId, userId, ct);
            var saved = await personalJourney.AdvanceAsync(progress with { SelectedObjectiveSlug = slug, CurrentStep = OnboardingStep.Availability }, progress.Version, ct);
            if (saved.IsFailure) TempData["Warning"] = saved.Error.Message;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Não foi possível persistir o foco do onboarding; mantendo fluxo básico"); }
        await audit.LogAsync("onboarding_objective_selected", "Objetivo selecionado no onboarding", userId: this.CurrentUserId(), email: User.Identity?.Name, metadata: new { slug }, ct: ct);
        return RedirectToAction(nameof(Templates), new { slug });
    }

    [ValidateAntiForgeryToken, HttpPost("/onboarding/focus")]
    public Task<IActionResult> Focus(string slug, CancellationToken ct) => SelectObjective(slug, ct);

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

    [ValidateAntiForgeryToken, HttpPost("/onboarding/skip")]
    public async Task<IActionResult> Skip(int? version, CancellationToken ct)
    {
        if (version is not null)
        {
            var result = await personalJourney.SkipAsync(this.CurrentClientId(), this.CurrentUserId(), version.Value, ct);
            if (result.IsFailure) TempData["Warning"] = result.Error.Message;
        }
        await audit.LogAsync("onboarding_skipped", "Onboarding pulado pelo usuário", userId: this.CurrentUserId(), email: User.Identity?.Name, ct: ct);
        TempData["Success"] = "Tudo bem. Você pode retomar a configuração quando quiser.";
        return RedirectToAction("Index", "MyDay");
    }
}
