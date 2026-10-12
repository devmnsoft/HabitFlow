using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("priorities")]
public sealed class PrioritiesController(PersonalPriorityService service, UserTimeZoneService timeZone) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = timeZone.Today();
        var model = await service.BuildAsync(this.CurrentClientId(), this.CurrentUserId(), today, ct);
        return View(new PrioritiesViewModel(model.Priorities, model.ActiveGoals, today));
    }

    [HttpPost("save"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PriorityFormModel form, CancellationToken ct)
    {
        var items = (form.GoalIds ?? [])
            .Where(x => x != Guid.Empty)
            .Select((goalId, index) => new PriorityConfigurationItem(goalId, index + 1, form.WeeklyAvailableMinutes))
            .ToList();
        await service.SaveAsync(this.CurrentClientId(), this.CurrentUserId(), items, timeZone.Today(), ct);
        TempData["Success"] = "Prioridades atualizadas. Elas orientam a apresentação sem alterar metas ou agenda.";
        return RedirectToAction(nameof(Index));
    }
}
