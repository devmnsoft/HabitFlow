using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("routine-sessions")]
public sealed class RoutineSessionsController(RoutineSessionService service, UserTimeZoneService timeZone) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? availableMinutes, [FromQuery] Guid[]? habitIds, CancellationToken ct)
    {
        var active = await service.GetActiveAsync(this.CurrentClientId(), this.CurrentUserId(), ct);
        var proposal = await service.ProposeAsync(this.CurrentClientId(), this.CurrentUserId(), availableMinutes, habitIds, ct);
        return View(new RoutineSessionPrepareViewModel("Sessão de rotina", availableMinutes, proposal, active));
    }

    [HttpPost("prepare"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Prepare(PrepareRoutineSessionForm form, CancellationToken ct)
    {
        var accountSession = User.FindFirst("session_id")?.Value;
        var result = await service.PrepareAsync(new(this.CurrentClientId(), this.CurrentUserId(), form.Name ?? "Sessão de rotina",
            form.AvailableMinutes, form.HabitIds.Select(x => new RoutineSessionPrepareItem(x)).ToList(),
            StableKey(form.IdempotencyKey), HttpContext.TraceIdentifier, accountSession), ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.Error.Message;
            return RedirectToAction(nameof(Index), new { form.AvailableMinutes });
        }
        TempData["Success"] = result.Value!.Message;
        return RedirectToAction(nameof(Show), new { id = result.Value.Details.Session.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken ct)
    {
        var details = await service.GetAsync(this.CurrentClientId(), this.CurrentUserId(), id, ct);
        if (details is null) return NotFound();
        return View(new RoutineSessionDetailsViewModel(details, BuildResumeMessage(details)));
    }

    [HttpPost("{id:guid}/start"), ValidateAntiForgeryToken]
    public Task<IActionResult> Start(Guid id, RoutineSessionCommandForm form, CancellationToken ct) =>
        Transition(id, form, (cmd, token) => service.StartAsync(cmd, token), ct);

    [HttpPost("{id:guid}/pause"), ValidateAntiForgeryToken]
    public Task<IActionResult> Pause(Guid id, RoutineSessionCommandForm form, CancellationToken ct) =>
        Transition(id, form, (cmd, token) => service.PauseAsync(cmd, token), ct);

    [HttpPost("{id:guid}/resume"), ValidateAntiForgeryToken]
    public Task<IActionResult> Resume(Guid id, RoutineSessionCommandForm form, CancellationToken ct) =>
        Transition(id, form, (cmd, token) => service.ResumeAsync(cmd, token), ct);

    [HttpPost("{id:guid}/cancel"), ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(Guid id, RoutineSessionCommandForm form, CancellationToken ct) =>
        Transition(id, form, (cmd, token) => service.CancelAsync(cmd, token), ct);

    [HttpPost("{id:guid}/items/{itemId:guid}/minimum"), ValidateAntiForgeryToken]
    public Task<IActionResult> Minimum(Guid id, Guid itemId, RoutineSessionItemForm form, CancellationToken ct) =>
        Item(id, itemId, form, (cmd, token) => service.RecordMinimumAsync(cmd, token), ct);

    [HttpPost("{id:guid}/items/{itemId:guid}/complete"), ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(Guid id, Guid itemId, RoutineSessionItemForm form, CancellationToken ct) =>
        Item(id, itemId, form, (cmd, token) => service.CompleteItemAsync(cmd, token), ct);

    [HttpPost("{id:guid}/items/{itemId:guid}/skip"), ValidateAntiForgeryToken]
    public Task<IActionResult> Skip(Guid id, Guid itemId, RoutineSessionItemForm form, CancellationToken ct) =>
        Item(id, itemId, form, (cmd, token) => service.SkipItemAsync(cmd, form.Reason, token), ct);

    [HttpPost("{id:guid}/close"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CloseRoutineSessionForm form, CancellationToken ct)
    {
        var result = await service.CloseAsync(new(this.CurrentClientId(), this.CurrentUserId(), id, form.Reflection,
            StableKey(form.IdempotencyKey), HttpContext.TraceIdentifier), ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? result.Value!.Message : result.Error.Message;
        return RedirectToAction(nameof(Show), new { id });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(DateOnly? from, DateOnly? to, string? status, int page = 1, CancellationToken ct = default)
    {
        var sessions = await service.HistoryAsync(this.CurrentClientId(), this.CurrentUserId(), from, to, status, page, 20, ct);
        return View(new RoutineSessionHistoryViewModel(sessions, from, to, status, Math.Max(1, page)));
    }

    private async Task<IActionResult> Transition(Guid id, RoutineSessionCommandForm form, Func<RoutineSessionCommand, CancellationToken, Task<HabitFlow.Shared.Result<RoutineSessionResult>>> action, CancellationToken ct)
    {
        var result = await action(new(this.CurrentClientId(), this.CurrentUserId(), id, StableKey(form.IdempotencyKey), HttpContext.TraceIdentifier), ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? result.Value!.Message : result.Error.Message;
        return RedirectToAction(nameof(Show), new { id });
    }

    private async Task<IActionResult> Item(Guid id, Guid itemId, RoutineSessionItemForm form, Func<RoutineSessionItemCommand, CancellationToken, Task<HabitFlow.Shared.Result<RoutineSessionResult>>> action, CancellationToken ct)
    {
        var result = await action(new(this.CurrentClientId(), this.CurrentUserId(), id, itemId, timeZone.Today(),
            form.Quantity, form.Unit, form.EffectiveMinutes, StableKey(form.IdempotencyKey), HttpContext.TraceIdentifier), ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? result.Value!.Message : result.Error.Message;
        return RedirectToAction(nameof(Show), new { id });
    }

    private static string StableKey(string? value) => string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim();

    private static string BuildResumeMessage(RoutineSessionDetails details) =>
        details.Session.Status switch
        {
            "Paused" => "Você pode continuar do próximo item, revisar os restantes ou encerrar parcialmente.",
            "Prepared" => "Revise os itens. Iniciar a sessão não conclui hábitos automaticamente.",
            "InProgress" => "Estado confirmado pelo servidor. Registre mínimo, alvo completo ou pule itens sem alterar a recorrência.",
            "Closed" => details.Session.ClosureSummary ?? "Sessão encerrada.",
            _ => "Sessão finalizada."
        };
}
