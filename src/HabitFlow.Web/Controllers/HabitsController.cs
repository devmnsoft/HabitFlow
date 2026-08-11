using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("habits")]
public sealed class HabitsController(HabitQueryService queries, HabitEditorService editor, HabitLifecycleService lifecycle,
    CompleteHabitUseCase completeHabit, UndoHabitCompletionUseCase undoHabit, UserTimeZoneService timeZone,
    ILogger<HabitsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] HabitListQuery query, CancellationToken ct)
    {
        if (!TryIdentity(out var clientId, out var userId)) return Forbid();
        return View(await queries.SearchAsync(clientId, userId, query, ct));
    }

    [HttpGet("create")]
    public IActionResult Create() => TryIdentity(out _, out _) ? View("Editor", EmptyEditor()) : Forbid();

    [HttpPost("create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HabitEditorViewModel model, CancellationToken ct)
    {
        if (!TryIdentity(out _, out _)) return Forbid();
        try
        {
            var result = await editor.SaveAsync(this.CurrentUserSnapshot(), model with { Id = null }, ct);
            if (result.IsFailure) { AddEditorError(result.Error.Code, result.Error.Message); return View("Editor", model); }
            TempData["Success"] = "Hábito criado. O próximo passo já pode começar.";
            TempData["HabitCreated"] = "true";
            return RedirectToAction(nameof(Detail), new { id = result.Value!.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha inesperada ao criar hábito. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar o hábito agora. Tente novamente.");
            return View("Editor", model);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        if (!TryIdentity(out var clientId, out var userId)) return Forbid();
        var model = await queries.DetailAsync(clientId, userId, id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!TryIdentity(out var clientId, out var userId)) return Forbid();
        var model = await editor.LoadAsync(clientId, userId, id, ct);
        return model is null ? NotFound() : View("Editor", model);
    }

    [HttpPost("{id:guid}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HabitEditorViewModel model, CancellationToken ct)
    {
        if (!TryIdentity(out _, out _)) return Forbid();
        try
        {
            var result = await editor.SaveAsync(this.CurrentUserSnapshot(), model with { Id = id }, ct);
            if (result.IsFailure) { if (result.Error.Code == "habit.not_found") return NotFound(); AddEditorError(result.Error.Code, result.Error.Message); return View("Editor", model with { Id = id }); }
            TempData["Success"] = "Alterações salvas."; return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha inesperada ao editar hábito {HabitId}. CorrelationId={CorrelationId}", id, HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar o hábito agora. Tente novamente.");
            return View("Editor", model with { Id = id });
        }
    }

    [HttpPost("{id:guid}/{actionName:regex(^pause|resume|archive|restore$)}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Lifecycle(Guid id, string actionName, CancellationToken ct)
    {
        if (!TryIdentity(out var clientId, out var userId)) return Forbid();
        var result = actionName switch { "pause" => await lifecycle.PauseAsync(clientId, userId, User.Identity?.Name, id, ct), "resume" => await lifecycle.ResumeAsync(clientId, userId, User.Identity?.Name, id, ct), "archive" => await lifecycle.ArchiveAsync(clientId, userId, User.Identity?.Name, id, ct), _ => await lifecycle.RestoreAsync(clientId, userId, User.Identity?.Name, id, ct) };
        if (result.IsFailure && result.Error.Code == "habit.not_found") return NotFound();
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Status do hábito atualizado." : result.Error.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/complete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct) => await CompletionAsync(id, true, ct);

    [HttpPost("{id:guid}/undo-completion"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(Guid id, CancellationToken ct) => await CompletionAsync(id, false, ct);

    private async Task<IActionResult> CompletionAsync(Guid id, bool complete, CancellationToken ct)
    {
        if (!TryIdentity(out var clientId, out var userId)) return Forbid();
        try
        {
            var command = new HabitCompletionCommand(clientId, userId, id, timeZone.Today(), Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"), "Habits", HttpContext.TraceIdentifier);
            var result = complete ? await completeHabit.ExecuteAsync(command, ct) : await undoHabit.ExecuteAsync(command, ct);
            if (result.IsFailure && result.Error.Code == "habit.not_found") return NotFound();
            if (Request.Headers.Accept.Any(x => x?.Contains("application/json") == true)) return result.IsSuccess ? Json(ToPayload(result.Value!, complete ? "Hábito concluído." : "Conclusão desfeita.")) : BadRequest(new { success = false, message = result.Error.Message });
            TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? (complete ? "Hábito concluído." : "Conclusão desfeita.") : result.Error.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex) { logger.LogError(ex, "Falha ao alterar conclusão do hábito {HabitId}", id); TempData["Error"] = "Não foi possível atualizar o hábito."; return RedirectToAction(nameof(Detail), new { id }); }
    }

    private bool TryIdentity(out Guid clientId, out Guid userId) { clientId = this.CurrentClientId(); userId = this.CurrentUserId(); return clientId != Guid.Empty && userId != Guid.Empty; }
    private void AddEditorError(string code, string message)
    {
        var field = code switch { "habit.custom_days_required" or "habit.weekday_invalid" => "SelectedDays", "habit.target_invalid" => "TargetPerWeek", "habit.frequency_invalid" => "FrequencyType", "habit.name" => "Name", "habit.color" => "Color", "habit.duration" => "EstimatedTimeMinutes", "habit.difficulty" => "Difficulty", "habit.objective_not_found" => "ObjectiveId", _ => string.Empty };
        ModelState.AddModelError(field, message);
    }
    private static HabitEditorViewModel EmptyEditor() => new(null, "", "#10B981", null, "check-circle", HabitFlow.Domain.HabitFrequencyType.Daily, null, null, null, [], null, 10, null);
    private static object ToPayload(HabitCompletionResult value, string message) => new
    {
        success = true, message, value.HabitId, date = value.Date.ToString("yyyy-MM-dd"), value.Completed,
        daily = new { scheduled = value.DailySummary.Scheduled, completed = value.DailySummary.Completed, pending = value.DailySummary.Pending, percentage = value.DailySummary.Percentage },
        streak = new { current = value.CurrentStreak, best = value.BestStreak }, value.NextHabit, value.GoalUpdates, value.NewMilestones
    };
}
