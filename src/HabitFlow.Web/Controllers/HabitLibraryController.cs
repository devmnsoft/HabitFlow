using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class HabitLibraryController(HabitLibraryService library, IHabitObjectiveRepository objectives, ILogger<HabitLibraryController> logger) : Controller
{
    [HttpGet("/habit-library")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await library.GetObjectivesAsync(ct);
        if (result.IsFailure) TempData["Error"] = result.Error.Message;
        return View(result.Value ?? Array.Empty<HabitObjective>());
    }

    [HttpGet("/habit-library/objective/{slug}")]
    public async Task<IActionResult> Objective(string slug, CancellationToken ct)
    {
        var objective = await objectives.GetBySlugAsync(slug, ct);
        if (objective is null) return NotFound();
        var templates = await library.GetTemplatesByObjectiveAsync(slug, ct);
        if (templates.IsFailure) TempData["Error"] = templates.Error.Message;
        return View((objective, Templates: templates.Value ?? Array.Empty<HabitTemplate>()));
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/add")]
    public async Task<IActionResult> Add(Guid templateId, string? returnUrl, CancellationToken ct)
    {
        try
        {
            var result = await library.AddTemplateToUserHabitsAsync(this.CurrentUserSnapshot(), templateId, ct);
            if (result.IsFailure) TempData["Error"] = result.Error.Message; else TempData["Success"] = "Hábito adicionado. Você já pode começar hoje.";
        }
        catch (Exception ex) { logger.LogError(ex, "Erro no POST /habit-library/add"); TempData["Error"] = "Não foi possível concluir agora. Tente novamente em instantes."; }
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/dashboard");
    }
}
