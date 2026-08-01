using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Controllers;

public sealed class HabitLibraryController(HabitLibraryService library, HabitTemplateFavoriteService favorites, IHabitObjectiveRepository objectives, IUserGoalRepository goals, IHabitRepository habits, PlanEntitlementService entitlements, CreateHabitFromTemplateUseCase createFromTemplate, IUserFacingErrorMapper errorMapper, ILogger<HabitLibraryController> logger) : Controller
{
    [HttpGet("/habit-library")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await library.GetObjectivesAsync(ct);
        if (result.IsFailure) { TempData["Warning"] = errorMapper.ToPublicMessage(result.Error.Code, "habit-library"); ViewData["UsingFallback"] = true; }
        return View(result.Value?.Any() == true ? result.Value : HabitLibraryFallback.Objectives);
    }

    [HttpGet("/habit-library/template/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await library.GetTemplateAsync(id, ct);
        if (result.IsFailure || result.Value!.PublishedAt is null) return NotFound();
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        var favorite = User.Identity?.IsAuthenticated == true && clientId != Guid.Empty && userId != Guid.Empty &&
            await favorites.IsFavoriteAsync(clientId, userId, id, ct);
        return View(new HabitTemplateDetailsViewModel(result.Value, favorite));
    }

    [Authorize]
    [HttpGet("/habit-library/template/{id:guid}/customize")]
    public async Task<IActionResult> Customize(Guid id, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        var result = await library.GetTemplateAsync(id, ct);
        if (result.IsFailure || result.Value!.PublishedAt is null) return NotFound();
        return View(await BuildCustomizationAsync(result.Value, clientId, userId, ct));
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/template/{id:guid}/customize")]
    public async Task<IActionResult> Customize(Guid id, CustomizeHabitTemplateViewModel model, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        if (id != model.TemplateId) return BadRequest();
        var template = await library.GetTemplateAsync(id, ct);
        if (template.IsFailure || template.Value!.PublishedAt is null) return NotFound();
        var command = new CreateHabitFromTemplateCommand(clientId, userId, id, model.Name, model.FrequencyType,
            model.TargetPerWeek, model.SelectedDays ?? [], model.PreferredTime, model.Color, model.Category,
            model.Notes, model.StartDate, model.ExistingGoalId, model.CreateGoal, model.GoalTitle,
            model.GoalTargetType, model.GoalTargetValue, model.AllowVariation, model.IsOnboarding ? "Onboarding" : null,
            model.CollectionId, model.IdempotencyKey, HttpContext.TraceIdentifier);
        var result = await createFromTemplate.ExecuteAsync(command, ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, errorMapper.ToPublicMessage(result.Error.Code));
            await HydrateCustomizationAsync(model, template.Value, clientId, userId, ct);
            return View(model);
        }
        TempData["Success"] = result.Value!.Message;
        return Redirect("/dashboard");
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/template/{id:guid}/favorite")]
    public Task<IActionResult> Favorite(Guid id, CancellationToken ct) => SetFavorite(id, true, ct);

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/template/{id:guid}/unfavorite")]
    public Task<IActionResult> Unfavorite(Guid id, CancellationToken ct) => SetFavorite(id, false, ct);

    [Authorize]
    [HttpGet("/habit-library/favorites")]
    public async Task<IActionResult> Favorites(CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        return View(await favorites.ListAsync(clientId, userId, ct));
    }

    private async Task<IActionResult> SetFavorite(Guid id, bool value, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        var result = await favorites.SetAsync(clientId, userId, id, value, ct);
        if (result.IsFailure) return result.Error.Code == "library.tenant_required" ? Forbid() : NotFound();
        if (Request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Json(new { favorite = value, message = value ? "Adicionado aos favoritos." : "Removido dos favoritos." });
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("/habit-library/objective/{slug}")]
    public async Task<IActionResult> Objective(string slug, CancellationToken ct)
    {
        HabitObjective? objective;
        try { objective = await objectives.GetBySlugAsync(slug, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao obter objetivo {Slug}", slug); objective = HabitLibraryFallback.Objectives.FirstOrDefault(o => o.Slug == slug); ViewData["UsingFallback"] = true; }
        if (objective is null) objective = HabitLibraryFallback.Objectives.FirstOrDefault(o => o.Slug == slug);
        if (objective is null) return NotFound();
        var templates = await library.GetTemplatesByObjectiveAsync(slug, ct);
        if (templates.IsFailure) { TempData["Warning"] = errorMapper.ToPublicMessage(templates.Error.Code, "habit-library"); ViewData["UsingFallback"] = true; }
        return View((objective, Templates: templates.Value?.Any() == true ? templates.Value : HabitLibraryFallback.TemplatesFor(objective.Id, slug)));
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/add")]
    public IActionResult Add(Guid templateId, string? returnUrl, CancellationToken ct)
    {
        return RedirectToAction("Customize", new { id = templateId });
    }

    private async Task<CustomizeHabitTemplateViewModel> BuildCustomizationAsync(HabitTemplate template, Guid clientId, Guid userId, CancellationToken ct)
    {
        var model = new CustomizeHabitTemplateViewModel
        {
            TemplateId = template.Id, TemplateName = template.Name, TemplateDescription = template.Description,
            Benefit = template.BenefitText, FirstAction = template.FirstAction, EstimatedTimeMinutes = template.EstimatedTimeMinutes,
            Difficulty = template.Difficulty, MinimumPlanCode = template.MinimumPlanCode, Name = template.Name,
            FrequencyType = Enum.TryParse<HabitFrequencyType>(template.SuggestedFrequency, true, out var frequency) ? frequency : HabitFrequencyType.Daily,
            TargetPerWeek = template.SuggestedTargetPerWeek, SelectedDays = SuggestedDays(template),
            PreferredTime = template.SuggestedReminderTime, Color = template.SuggestedColor,
            Category = template.Category, StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        await HydrateCustomizationAsync(model, template, clientId, userId, ct);
        return model;
    }

    private async Task HydrateCustomizationAsync(CustomizeHabitTemplateViewModel model, HabitTemplate template, Guid clientId, Guid userId, CancellationToken ct)
    {
        model.TemplateName = template.Name; model.TemplateDescription = template.Description; model.Benefit = template.BenefitText;
        model.FirstAction = template.FirstAction; model.EstimatedTimeMinutes = template.EstimatedTimeMinutes;
        model.Difficulty = template.Difficulty; model.MinimumPlanCode = template.MinimumPlanCode;
        model.AvailableGoals = await goals.ListAsync(clientId, userId, ct);
        model.AllowedColors = HabitTemplateCustomizationValidator.AllowedColors;
        var active = (await habits.ListByUserAsync(userId, ct)).Count(h => !h.IsArchived && h.ClientId == clientId);
        var limit = await entitlements.GetIntegerFeatureAsync(userId, PlanFeatureCodes.ActiveHabitsLimit, ct);
        model.PlanUsage = new(active, limit, limit is null or < 0 ? int.MaxValue : Math.Max(0, limit.Value - active));
        model.PlanLimitReached = limit is >= 0 && active >= limit;
    }

    private static int[] SuggestedDays(HabitTemplate template) => Enumerable.Range(0, 7)
        .Where(day => template.IsSuggestedOn((DayOfWeek)day)).ToArray();
}
