using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Controllers;

public sealed class HabitLibraryController(HabitLibraryService library, HabitTemplateFavoriteService favorites, IHabitObjectiveRepository objectives, IUserGoalRepository goals, IHabitRepository habits, PlanEntitlementService entitlements, CreateHabitFromTemplateUseCase createFromTemplate, IUserFacingErrorMapper errorMapper, ILogger<HabitLibraryController> logger, HabitTemplateCollectionService collections, ActivateHabitCollectionUseCase activateCollection) : Controller
{
    [HttpGet("/habit-library/collections")]
    public async Task<IActionResult> Collections(CancellationToken ct) => View(await collections.ListAsync(ct));

    [HttpGet("/habit-library/collection/{slug}")]
    [Authorize]
    public async Task<IActionResult> Collection(string slug, CancellationToken ct)
    {
        var result = await collections.GetAsync(slug, this.CurrentUserId(), ct);
        return result.IsFailure ? NotFound() : View(result.Value);
    }

    [HttpGet("/habit-library/collection/{slug}/customize")]
    [Authorize]
    public async Task<IActionResult> CustomizeCollection(string slug, CancellationToken ct)
    {
        var result = await collections.GetAsync(slug, this.CurrentUserId(), ct);
        if(result.IsFailure) return NotFound();
        return View(await BuildCollectionCustomizationAsync(result.Value!,ct));
    }

    [Authorize,ValidateAntiForgeryToken]
    [HttpPost("/habit-library/collection/{slug}/customize")]
    public async Task<IActionResult> ActivateCollection(string slug, CollectionCustomizationViewModel model, CancellationToken ct)
    {
        var details=await collections.GetAsync(slug,this.CurrentUserId(),ct);
        if(details.IsFailure || details.Value!.Collection.Id!=model.CollectionId) return NotFound();
        var command=new ActivateHabitCollectionCommand(this.CurrentClientId(),this.CurrentUserId(),details.Value.Collection.Id,
            model.Items.Select(x=>new HabitCollectionCustomization(x.TemplateId,x.Included,x.Name,x.FrequencyType,x.TargetPerWeek,
                x.SelectedDays,x.PreferredTime,x.Color,x.Category,x.StartDate)).ToList(),model.ExistingGoalId,model.CreateGoal,
            model.GoalTitle,model.GoalTargetType,model.GoalTargetValue,model.IdempotencyKey,HttpContext.TraceIdentifier,null,null);
        var result=await activateCollection.ExecuteAsync(command,ct);
        if(result.IsFailure){ModelState.AddModelError(string.Empty,errorMapper.ToPublicMessage(result.Error.Code));model=await BuildCollectionCustomizationAsync(details.Value,ct,model);return View("CustomizeCollection",model);}
        TempData["Success"]=result.Value!.Message;
        return RedirectToAction("Index","Dashboard");
    }
    [HttpGet("/habit-library")]
    public async Task<IActionResult> Index(string? focus, string? category, string? difficulty, string? duration, string? frequency, string? minimumPlan, bool favoritesOnly, CancellationToken ct)
    {
        var objectivesResult = await library.GetObjectivesAsync(ct);
        var templatesResult = await library.GetTemplatesAsync(ct);
        if (objectivesResult.IsFailure || templatesResult.IsFailure) { TempData["Warning"] = "A biblioteca completa não pôde ser carregada agora."; ViewData["UsingFallback"] = true; }
        var objectivesList = objectivesResult.Value?.Any() == true ? objectivesResult.Value : HabitLibraryFallback.Objectives;
        var templatesList = templatesResult.Value ?? [];
        IReadOnlySet<Guid> favoriteIds = new HashSet<Guid>();
        if (User.Identity?.IsAuthenticated == true && this.CurrentClientId() != Guid.Empty && this.CurrentUserId() != Guid.Empty)
            favoriteIds = (await favorites.ListAsync(this.CurrentClientId(), this.CurrentUserId(), ct)).Select(x => x.Id).ToHashSet();
        return View(new HabitLibraryIndexViewModel(objectivesList, templatesList, favoriteIds, focus, category, difficulty, duration, frequency, minimumPlan, favoritesOnly));
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

    [HttpGet("/habit-library/templates/{id:guid}")]
    public Task<IActionResult> TemplateDetails(Guid id, CancellationToken ct) => Details(id, ct);

    [Authorize, HttpGet("/habit-library/templates/{id:guid}/customize")]
    public Task<IActionResult> TemplateCustomize(Guid id, bool onboarding, CancellationToken ct) => Customize(id, onboarding, ct);

    [Authorize, ValidateAntiForgeryToken, HttpPost("/habit-library/templates/{id:guid}/use")]
    public IActionResult Use(Guid id) => RedirectToAction(nameof(Customize), new { id });

    [Authorize, ValidateAntiForgeryToken, HttpPost("/habit-library/templates/{id:guid}/favorite")]
    public Task<IActionResult> TemplateFavorite(Guid id, CancellationToken ct) => SetFavorite(id, true, ct);

    [Authorize]
    [HttpGet("/habit-library/template/{id:guid}/customize")]
    public async Task<IActionResult> Customize(Guid id, bool onboarding, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        var result = await library.GetTemplateAsync(id, ct);
        if (result.IsFailure || result.Value!.PublishedAt is null) return NotFound();
        var model = await BuildCustomizationAsync(result.Value, clientId, userId, ct);
        model.IsOnboarding = onboarding;
        model.ReturnUrl = onboarding ? "/onboarding" : "/habit-library";
        return View(model);
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

    private async Task<CollectionCustomizationViewModel> BuildCollectionCustomizationAsync(HabitTemplateCollectionDetails details,CancellationToken ct,CollectionCustomizationViewModel? posted=null)
    {
        var active=await habits.CountActiveAsync(this.CurrentClientId(),this.CurrentUserId(),ct);
        var limit=await entitlements.GetIntegerFeatureAsync(this.CurrentUserId(),PlanFeatureCodes.ActiveHabitsLimit,ct);
        return new CollectionCustomizationViewModel { Details=details, CollectionId=details.Collection.Id, Items=posted?.Items??details.Items.Select(x=>new CollectionCustomizationItemViewModel
            {TemplateId=x.TemplateId,Included=true,Name=x.Template.Name,FrequencyType=Enum.TryParse<HabitFrequencyType>(x.Template.SuggestedFrequency,true,out var f)?f:HabitFrequencyType.Daily,
             TargetPerWeek=x.Template.SuggestedTargetPerWeek,SelectedDays=SuggestedDays(x.Template),PreferredTime=x.DefaultReminderTime??x.Template.SuggestedReminderTime,
             Color=x.Template.SuggestedColor,Category=x.Template.Category,StartDate=DateOnly.FromDateTime(DateTime.UtcNow)}).ToList(),
            ExistingGoalId=posted?.ExistingGoalId,CreateGoal=posted?.CreateGoal??false,GoalTitle=posted?.GoalTitle,GoalTargetType=posted?.GoalTargetType,
            GoalTargetValue=posted?.GoalTargetValue,IdempotencyKey=posted?.IdempotencyKey??Guid.NewGuid(),Goals=await goals.ListAsync(this.CurrentClientId(),this.CurrentUserId(),ct),
            PlanUsage=new(active,limit,limit is null or <0?int.MaxValue:Math.Max(0,limit.Value-active))};
    }
}
