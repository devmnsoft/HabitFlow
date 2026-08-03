using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("weekly-review")]
public sealed class WeeklyReviewController(WeeklyReviewService service,CompleteWeeklyReviewUseCase complete,UserTimeZoneService timeZone) : Controller
{
    [HttpGet("")]
    public Task<IActionResult> Index(CancellationToken ct) { var today=timeZone.Today(); var start=today.AddDays(-(((int)today.DayOfWeek+6)%7)); return Show(start,ct); }
    [HttpGet("{periodStart}")]
    public async Task<IActionResult> Show(DateOnly periodStart,CancellationToken ct) => View("Index",await service.BuildAsync(this.CurrentClientId(),this.CurrentUserId(),periodStart,ct));
    [HttpPost("{periodStart}/complete"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(DateOnly periodStart,string idempotencyKey,CancellationToken ct) { await complete.ExecuteAsync(this.CurrentClientId(),this.CurrentUserId(),periodStart,idempotencyKey,ct); TempData["Success"]="Revisão concluída. Você pode ajustar o caminho sempre que precisar."; return RedirectToAction(nameof(Show),new{periodStart}); }
}
