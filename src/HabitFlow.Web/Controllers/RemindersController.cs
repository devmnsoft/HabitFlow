using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class RemindersController(HabitReminderService reminders) : Controller
{
    [HttpGet("/reminders")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new ReminderListViewModel(await reminders.ListAsync(this.CurrentClientId(), this.CurrentUserId(), null, ct)));

    [HttpGet("/habits/{habitId:guid}/reminders")]
    public async Task<IActionResult> Habit(Guid habitId, CancellationToken ct)
    {
        var existing = await reminders.ListAsync(this.CurrentClientId(), this.CurrentUserId(), habitId, ct);
        if (existing.Count == 0) return View(new HabitReminderEditorViewModel { HabitId = habitId });
        return View(new HabitReminderEditorViewModel { HabitId = habitId, HabitName = existing[0].HabitName, Existing = existing });
    }

    [ValidateAntiForgeryToken, HttpPost("/habits/{habitId:guid}/reminders")]
    public async Task<IActionResult> Create(Guid habitId, HabitReminderEditorViewModel model, CancellationToken ct)
    {
        model.HabitId = habitId;
        if (!ModelState.IsValid) return View("Habit", model);
        var result = await reminders.CreateAsync(this.CurrentClientId(), this.CurrentUserId(), habitId, model.ReminderTime, model.Days, model.Timezone, ct);
        if (result.IsFailure) { ModelState.AddModelError("", result.Error.Message); return View("Habit", model); }
        TempData["Success"] = "Lembrete configurado.";
        return RedirectToAction(nameof(Habit), new { habitId });
    }

    [ValidateAntiForgeryToken, HttpPost("/reminders/{id:guid}/pause")]
    public Task<IActionResult> Pause(Guid id, CancellationToken ct) => Change(id, false, ct);
    [ValidateAntiForgeryToken, HttpPost("/reminders/{id:guid}/resume")]
    public Task<IActionResult> Resume(Guid id, CancellationToken ct) => Change(id, true, ct);
    [ValidateAntiForgeryToken, HttpPost("/reminders/{id:guid}/delete")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) => Change(id, false, ct);

    private async Task<IActionResult> Change(Guid id, bool active, CancellationToken ct)
    {
        var result = await reminders.SetActiveAsync(this.CurrentClientId(), this.CurrentUserId(), id, active, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? (active ? "Lembrete reativado." : "Lembrete pausado.") : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
