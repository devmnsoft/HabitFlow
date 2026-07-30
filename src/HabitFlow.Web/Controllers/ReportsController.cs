using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class ReportsController(ReportService reports, AuditService audit, UserTimeZoneService timeZones, CurrentTenantService tenant) : Controller
{
    [HttpGet("reports")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = timeZones.Today();
        await audit.LogAsync("report_viewed", "Relatórios visualizados", HabitFlow.Domain.AuditSeverity.Info, this.CurrentUserId(), null, null, ct);
        return View(await reports.BuildWeeklyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), today, ct));
    }
    [HttpGet("reports/weekly")]
    public async Task<IActionResult> Weekly(CancellationToken ct) => Json(await reports.BuildWeeklyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), timeZones.Today(), ct));
    [HttpGet("reports/monthly")]
    public async Task<IActionResult> Monthly(CancellationToken ct) { var now = timeZones.Today(); return Json(await reports.BuildMonthlyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), now.Year, now.Month, ct)); }
    [HttpGet("reports/export-csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct) { var end = timeZones.Today(); var result = await reports.ExportPersonalReportCsvAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), end.AddDays(-30), end, ct); if (result.IsFailure) return BadRequest(result.Error.Message); return File(result.Value!, "text/csv; charset=utf-8", $"habitflow-relatorio-{end:yyyy-MM}.csv"); }
    [HttpGet("reports/print")]
    public async Task<IActionResult> Print(CancellationToken ct)
    {
        var end = timeZones.Today();
        return View(await reports.BuildMonthlyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), end.Year, end.Month, ct));
    }
}
