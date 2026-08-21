using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class ReportsController(ReportService reports, WeeklyReviewService weeklyReviews, ReportDocumentExporter documents,
    FeatureAccessService access, AuditService audit, UserTimeZoneService timeZones, CurrentTenantService tenant,
    ILogger<ReportsController> logger) : Controller
{
    private static readonly EventId PlanBlocked = new(616601, "report.plan_blocked");
    private static readonly EventId PdfExported = new(616602, "report.pdf.exported");
    private static readonly EventId CsvExported = new(616603, "report.csv.exported");
    [HttpGet("reports")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = timeZones.Today();
        await audit.LogAsync("report_viewed", "Relatórios visualizados", HabitFlow.Domain.AuditSeverity.Info, this.CurrentUserId(), null, null, ct);
        return View(await reports.BuildWeeklyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), today, ct));
    }
    [HttpGet("reports/weekly")]
    public async Task<IActionResult> Weekly(CancellationToken ct)
    {
        ViewData["ReportPeriod"] = "weekly";
        return View("Index", await reports.BuildWeeklyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), timeZones.Today(), ct));
    }
    [HttpGet("reports/monthly")]
    public async Task<IActionResult> Monthly([FromQuery] string? month, CancellationToken ct)
    {
        var now = timeZones.Today();
        if (!string.IsNullOrWhiteSpace(month) && (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out now) || now < new DateOnly(2000, 1, 1) || now > timeZones.Today().AddMonths(1)))
            return BadRequest("Informe o mês no formato AAAA-MM.");
        ViewData["ReportPeriod"] = "monthly";
        return View("Index", await reports.BuildMonthlyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), now.Year, now.Month, ct));
    }
    [HttpGet("reports/export")]
    [HttpGet("reports/export-csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        if (!await RequirePremiumAsync(HabitFlow.Domain.PlanFeatureCodes.ReportExportCsv, ct)) return Redirect("/plans?from=csv_export");
        var end = timeZones.Today(); var start = end.AddDays(-(((int)end.DayOfWeek + 6) % 7));
        var bytes = documents.ToCsv(await weeklyReviews.BuildAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), start, ct));
        logger.LogInformation(CsvExported, "report.csv.exported CorrelationId={CorrelationId} UserId={UserId}", HttpContext.TraceIdentifier, this.CurrentUserId());
        return File(bytes, "text/csv; charset=utf-8", $"habitflow-dados-{end:yyyy-MM-dd}.csv");
    }
    [HttpGet("reports/monthly/export/pdf")]
    [HttpGet("reports/export-pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] string? month, CancellationToken ct)
    {
        if (!await RequirePremiumAsync(HabitFlow.Domain.PlanFeatureCodes.ReportPrint, ct)) return Redirect("/plans?from=pdf_export");
        var selected = timeZones.Today();
        if (!string.IsNullOrWhiteSpace(month) && !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out selected)) return BadRequest("Informe o mês no formato AAAA-MM.");
        var report = await reports.BuildMonthlyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), selected.Year, selected.Month, ct);
        var bytes = documents.ToPdf(report, DateTimeOffset.UtcNow);
        logger.LogInformation(PdfExported, "report.pdf.exported CorrelationId={CorrelationId} UserId={UserId}", HttpContext.TraceIdentifier, this.CurrentUserId());
        return File(bytes, "application/pdf", $"habitflow-relatorio-{selected:yyyy-MM}.pdf");
    }
    [HttpGet("weekly-review/{weekStart}/export/pdf")]
    public async Task<IActionResult> ExportWeeklyPdf(DateOnly weekStart, CancellationToken ct)
    {
        if (!await RequirePremiumAsync(HabitFlow.Domain.PlanFeatureCodes.ReportPrint, ct)) return Redirect("/plans?from=pdf_export");
        if (weekStart.DayOfWeek != DayOfWeek.Monday || weekStart > timeZones.Today() || weekStart < timeZones.Today().AddYears(-2))
            return BadRequest("Escolha uma segunda-feira válida dos últimos dois anos.");
        var review = await weeklyReviews.BuildAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), weekStart, ct);
        var report = new PersonalReport(review.PeriodStart, review.PeriodEnd, review.Scheduled, review.Completed,
            review.Habits.Count(x => x.Completed > 0), review.Percentage,
            review.BestHabit is null ? "Uma semana boa não precisa ser perfeita. Precisa ser repetível." : $"Seu melhor ritmo foi em {review.BestHabit}.");
        var bytes = documents.ToPdf(report, DateTimeOffset.UtcNow);
        logger.LogInformation(PdfExported, "report.pdf.exported Period=weekly CorrelationId={CorrelationId} UserId={UserId}", HttpContext.TraceIdentifier, this.CurrentUserId());
        return File(bytes, "application/pdf", $"habitflow-revisao-{weekStart:yyyy-MM-dd}.pdf");
    }
    [HttpGet("reports/print")]
    public async Task<IActionResult> Print(CancellationToken ct)
    {
        var end = timeZones.Today();
        return View(await reports.BuildMonthlyReportAsync(tenant.RequireCurrentClientId(), this.CurrentUserId(), end.Year, end.Month, ct));
    }

    private async Task<bool> RequirePremiumAsync(string feature, CancellationToken ct)
    {
        var allowed = (await access.RequireFeatureAsync(this.CurrentUserId(), feature, ct)).Allowed;
        if (!allowed) logger.LogWarning(PlanBlocked, "report.plan_blocked Feature={Feature} CorrelationId={CorrelationId} UserId={UserId}", feature, HttpContext.TraceIdentifier, this.CurrentUserId());
        return allowed;
    }
}
