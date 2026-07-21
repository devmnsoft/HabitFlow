using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class HabitsController(HabitService habitService, ILogger<HabitsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await habitService.ListAsync(this.CurrentUserId(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar hábitos"); TempData["Error"] = "Não foi possível carregar hábitos."; return View(Array.Empty<HabitDto>()); }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(string name, string color, string? category, CancellationToken ct)
    {
        try
        {
            var result = await habitService.CreateAsync(this.CurrentUserSnapshot(), name, color, category, ct);
            TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Hábito criado.";
        }
        catch (Exception ex) { logger.LogError(ex, "Erro inesperado ao criar hábito"); TempData["Error"] = "Não foi possível criar o hábito."; }
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        try { var result = await habitService.MarkTodayAsync(this.CurrentUserSnapshot(), id, ct); TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Hábito marcado."; }
        catch (Exception ex) { logger.LogError(ex, "Erro inesperado ao marcar hábito {HabitId}", id); TempData["Error"] = "Não foi possível marcar o hábito."; }
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Uncomplete(Guid id, CancellationToken ct)
    {
        try { var result = await habitService.UnmarkTodayAsync(id, ct); TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Marcação removida."; }
        catch (Exception ex) { logger.LogError(ex, "Erro inesperado ao desmarcar hábito {HabitId}", id); TempData["Error"] = "Não foi possível desmarcar o hábito."; }
        return RedirectToAction(nameof(Index));
    }
}
