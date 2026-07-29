using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public class HabitsController(HabitService habitService, CompleteHabitUseCase completeHabit, UndoHabitCompletionUseCase undoHabit, UserTimeZoneService timeZone, AuditService audit, ILogger<HabitsController> logger) : Controller
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
        var user = this.CurrentUserSnapshot();
        if (!user.ClientId.HasValue) return Forbid();
        var result = await completeHabit.ExecuteAsync(new(user.ClientId.Value, user.Id, id, timeZone.Today(), Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"), "Dashboard", HttpContext.TraceIdentifier), ct);
        if (result.IsFailure && result.Error.Code == "habit.not_found") return NotFound(new { success = false, message = result.Error.Message });
        if (result.IsFailure) return BadRequest(new { success = false, message = result.Error.Message });
        return Json(ToPayload(result.Value!, "Um passo concluído. Continue no seu ritmo."));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("habits/{id:guid}/undo-completion")]
    public async Task<IActionResult> UndoWithoutReload(Guid id, CancellationToken ct)
    {
        var user = this.CurrentUserSnapshot();
        if (!user.ClientId.HasValue) return Forbid();
        var result = await undoHabit.ExecuteAsync(new(user.ClientId.Value, user.Id, id, timeZone.Today(), Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"), "Dashboard", HttpContext.TraceIdentifier), ct);
        if (result.IsFailure && result.Error.Code == "habit.not_found") return NotFound(new { success = false, message = result.Error.Message });
        if (result.IsFailure) return BadRequest(new { success = false, message = result.Error.Message });
        return Json(ToPayload(result.Value!, "Conclusão desfeita."));
    }

        [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Uncomplete(Guid id, CancellationToken ct)
    {
        try { var result = await habitService.UnmarkTodayAsync(this.CurrentUserSnapshot(), id, ct); TempData[result.IsFailure ? "Error" : "Success"] = result.IsFailure ? result.Error.Message : "Marcação removida."; }
        catch (Exception ex) { logger.LogError(ex, "Erro inesperado ao desmarcar hábito {HabitId}", id); TempData["Error"] = "Não foi possível desmarcar o hábito."; }
        return RedirectToAction(nameof(Index));
    }

    private static object ToPayload(HabitCompletionResult value, string message) => new
    {
        success = true, message, value.HabitId, date = value.Date.ToString("yyyy-MM-dd"), value.Completed,
        daily = new { scheduled = value.DailySummary.Scheduled, completed = value.DailySummary.Completed, pending = value.DailySummary.Pending, percentage = value.DailySummary.Percentage },
        streak = new { current = value.CurrentStreak, best = value.BestStreak }, value.NextHabit, value.GoalUpdates, value.NewMilestones
    };
}
