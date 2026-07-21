using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class HabitLibraryController(HabitLibraryService library, IHabitObjectiveRepository objectives, IUserFacingErrorMapper errorMapper, ILogger<HabitLibraryController> logger) : Controller
{
    [HttpGet("/habit-library")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await library.GetObjectivesAsync(ct);
        if (result.IsFailure) { TempData["Warning"] = errorMapper.ToPublicMessage(result.Error.Code, "habit-library"); ViewData["UsingFallback"] = true; }
        return View(result.Value?.Any() == true ? result.Value : HabitLibraryFallback.Objectives);
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
    public async Task<IActionResult> Add(Guid templateId, string? returnUrl, CancellationToken ct)
    {
        try
        {
            var result = await library.AddTemplateToUserHabitsAsync(this.CurrentUserSnapshot(), templateId, ct);
            if (result.IsFailure) TempData["Error"] = errorMapper.ToPublicMessage(result.Error.Code); else TempData["Success"] = "Hábito adicionado. Você já pode começar hoje.";
        }
        catch (Exception ex) { logger.LogError(ex, "Erro no POST /habit-library/add"); TempData["Error"] = "Não foi possível concluir agora. Tente novamente em instantes."; }
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/dashboard");
    }
}
