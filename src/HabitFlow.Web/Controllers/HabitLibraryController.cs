using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Controllers;

public sealed class HabitLibraryController(HabitLibraryService library, HabitTemplateFavoriteService favorites, IHabitObjectiveRepository objectives, IUserFacingErrorMapper errorMapper, ILogger<HabitLibraryController> logger) : Controller
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
        var favorite = User.Identity?.IsAuthenticated == true && CurrentClientId() != Guid.Empty &&
            await favorites.IsFavoriteAsync(CurrentClientId(), CurrentUserId(), id, ct);
        return View(new HabitTemplateDetailsViewModel(result.Value, favorite));
    }

    [Authorize]
    [HttpGet("/habit-library/template/{id:guid}/customize")]
    public async Task<IActionResult> Customize(Guid id, CancellationToken ct)
    {
        var result = await library.GetTemplateAsync(id, ct);
        if (result.IsFailure || result.Value!.PublishedAt is null) return NotFound();
        return View(result.Value);
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/habit-library/template/{id:guid}/customize")]
    public async Task<IActionResult> Customize(Guid id, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) ModelState.AddModelError(nameof(name), "Informe um nome com até 120 caracteres.");
        var template = await library.GetTemplateAsync(id, ct);
        if (template.IsFailure || template.Value!.PublishedAt is null) return NotFound();
        if (!ModelState.IsValid) return View(template.Value);
#pragma warning disable CS0618 // Transitional adapter remains a single delegated entry point during the use-case migration.
        var result = await library.AddTemplateToUserHabitsAsync(CurrentUserSnapshot(), id, ct, name);
#pragma warning restore CS0618
        if (result.IsFailure) { ModelState.AddModelError(string.Empty, errorMapper.ToPublicMessage(result.Error.Code)); return View(template.Value); }
        TempData["Success"] = "Hábito adicionado. Você já pode acompanhá-lo no Dashboard.";
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
        if (CurrentClientId() == Guid.Empty) return Forbid();
        return View(await favorites.ListAsync(CurrentClientId(), CurrentUserId(), ct));
    }

    private async Task<IActionResult> SetFavorite(Guid id, bool value, CancellationToken ct)
    {
        var result = await favorites.SetAsync(CurrentClientId(), CurrentUserId(), id, value, ct);
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
}
