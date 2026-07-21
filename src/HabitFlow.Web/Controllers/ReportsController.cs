using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class ReportsController(ReportService reports, AuditService audit) : Controller
{
    [HttpGet("reports")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await audit.LogAsync("report_viewed", "Relatórios visualizados", HabitFlow.Domain.AuditSeverity.Info, this.CurrentUserId(), null, null, ct);
        return View(await reports.BuildWeeklyReportAsync(this.CurrentUserId(), today, ct));
    }
    [HttpGet("reports/weekly")]
    public async Task<IActionResult> Weekly(CancellationToken ct) => Json(await reports.BuildWeeklyReportAsync(this.CurrentUserId(), DateOnly.FromDateTime(DateTime.UtcNow), ct));
    [HttpGet("reports/monthly")]
    public async Task<IActionResult> Monthly(CancellationToken ct) { var now = DateTime.UtcNow; return Json(await reports.BuildMonthlyReportAsync(this.CurrentUserId(), now.Year, now.Month, ct)); }
    [HttpGet("reports/export-csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct) { var end = DateOnly.FromDateTime(DateTime.UtcNow); var result = await reports.ExportPersonalReportCsvAsync(this.CurrentUserId(), end.AddDays(-30), end, ct); if (result.IsFailure) return BadRequest(result.Error.Message); return File(result.Value!, "text/csv; charset=utf-8", "habitflow-relatorio.csv"); }
}
