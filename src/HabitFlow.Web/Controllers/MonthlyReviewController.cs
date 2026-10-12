using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("monthly-review")]
public sealed class MonthlyReviewController(MonthlyReviewService service, UserTimeZoneService timeZone) : Controller
{
    [HttpGet("")]
    public Task<IActionResult> Index(CancellationToken ct)
    {
        var today = timeZone.Today();
        return Show($"{today:yyyy-MM}", ct);
    }

    [HttpGet("{period}")]
    public async Task<IActionResult> Show(string period, CancellationToken ct)
    {
        var start = ParsePeriod(period) ?? new DateOnly(timeZone.Today().Year, timeZone.Today().Month, 1);
        var result = await service.BuildAsync(this.CurrentClientId(), this.CurrentUserId(), start, ct);
        var form = new MonthlyReviewFormModel
        {
            Reflection = result.Draft?.Reflection,
            SelectedNextStep = result.Draft?.SelectedNextStep,
            ConfirmedPlanCode = result.Draft?.ConfirmedPlanCode,
            IdempotencyKey = result.Draft?.IdempotencyKey ?? Guid.NewGuid().ToString("N"),
            Version = result.Draft?.Version ?? 0
        };
        return View("Show", new MonthlyReviewViewModel(result, form));
    }

    [HttpPost("{period}/draft"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Draft(string period, MonthlyReviewFormModel form, CancellationToken ct)
    {
        var start = ParsePeriod(period) ?? new DateOnly(timeZone.Today().Year, timeZone.Today().Month, 1);
        await service.SaveDraftAsync(this.CurrentClientId(), this.CurrentUserId(), start, form.Reflection, form.SelectedNextStep, form.Version, form.IdempotencyKey, ct);
        TempData["Success"] = "Rascunho mensal salvo. Nenhuma rotina foi alterada.";
        return RedirectToAction(nameof(Show), new { period = $"{start:yyyy-MM}" });
    }

    [HttpPost("{period}/complete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(string period, MonthlyReviewFormModel form, CancellationToken ct)
    {
        var start = ParsePeriod(period) ?? new DateOnly(timeZone.Today().Year, timeZone.Today().Month, 1);
        await service.CompleteAsync(this.CurrentClientId(), this.CurrentUserId(), start, form.Reflection, form.SelectedNextStep, form.ConfirmedPlanCode, form.Version, form.IdempotencyKey, ct);
        TempData["Success"] = "Revisão mensal concluída. Apenas a proposta escolhida ficou registrada; hábitos não mudaram automaticamente.";
        return RedirectToAction(nameof(Show), new { period = $"{start:yyyy-MM}" });
    }

    private static DateOnly? ParsePeriod(string period) =>
        DateOnly.TryParse($"{period}-01", out var value) ? value : null;
}
