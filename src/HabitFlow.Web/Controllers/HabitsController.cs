using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class HabitsController(HabitService habitService, AuditService audit, ILogger<HabitsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await habitService.ListAsync(this.CurrentUserId(), ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar hábitos"); TempData["Error"] = "Não foi possível carregar hábitos."; return View(Array.Empty<HabitDto>()); }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(string name, string color, string? category, HabitFrequencyType frequencyType = HabitFrequencyType.Daily, int? targetPerWeek = null, TimeOnly? reminderTime = null, string? notes = null, int[]? selectedDays = null, CancellationToken ct = default)
    {
        try
        {
            var result = await habitService.CreateAsync(this.CurrentUserSnapshot(), name, color, category, frequencyType, targetPerWeek, reminderTime, notes, selectedDays ?? Array.Empty<int>(), ct);
            TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Hábito criado.";
        }
        catch (Exception ex) { logger.LogError(ex, "Erro inesperado ao criar hábito"); TempData["Error"] = "Não foi possível criar o hábito."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("habits/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var habits = await habitService.ListAsync(this.CurrentUserId(), ct);
        var habit = habits.FirstOrDefault(x => x.Id == id && x.BelongsTo(this.CurrentUserId()));
        if (habit is null) return NotFound();
        await audit.LogAsync("habit_detail_viewed", "Detalhe do hábito visualizado", AuditSeverity.Info, this.CurrentUserId(), null, new { habitId = id }, ct);
        return View(habit);
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
    [HttpPost("habits/{id:guid}/complete")]
    public async Task<IActionResult> CompleteWithoutReload(Guid id, CancellationToken ct)
    {
        var owned = (await habitService.ListAsync(this.CurrentUserId(), ct)).Any(x => x.Id == id);
        if (!owned) return NotFound(new { success = false, message = "Este hábito não foi encontrado." });
        var result = await habitService.MarkTodayAsync(this.CurrentUserSnapshot(), id, ct);
        return Json(new { success = result.IsSuccess, message = result.IsSuccess ? "Um passo concluído. Continue no seu ritmo." : result.Error.Message, completed = result.IsSuccess, dailyProgress = 0, currentStreak = 0, nextHabit = (object?)null });
    }

    [ValidateAntiForgeryToken]
    [HttpPost("habits/{id:guid}/undo-completion")]
    public async Task<IActionResult> UndoWithoutReload(Guid id, CancellationToken ct)
    {
        var owned = (await habitService.ListAsync(this.CurrentUserId(), ct)).Any(x => x.Id == id);
        if (!owned) return NotFound(new { success = false, message = "Este hábito não foi encontrado." });
        var result = await habitService.UnmarkTodayAsync(id, ct);
        return Json(new { success = result.IsSuccess, message = result.IsSuccess ? "Conclusão desfeita." : result.Error.Message, completed = false, dailyProgress = 0, currentStreak = 0, nextHabit = (object?)null });
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
