using System.Globalization;
using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record PersonalReport(DateOnly PeriodStart, DateOnly PeriodEnd, int TotalCompletions, int ActiveDays, double CompletionRate, string Insight);

public sealed class ReportService(IHabitRepository habits, IHabitCompletionRepository completions, IUserReportRepository reports, AuditService audit, ILogger<ReportService> logger)
{
    public async Task<PersonalReport> BuildWeeklyReportAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var start = date.AddDays(-((int)date.DayOfWeek));
        var report = await BuildAsync(userId, start, start.AddDays(6), ct);
        await audit.LogAsync("weekly_report_generated", "Relatório semanal gerado", AuditSeverity.Info, userId, null, new { start }, ct);
        return report;
    }
    public async Task<PersonalReport> BuildMonthlyReportAsync(Guid userId, int year, int month, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var report = await BuildAsync(userId, start, start.AddMonths(1).AddDays(-1), ct);
        await audit.LogAsync("monthly_report_generated", "Relatório mensal gerado", AuditSeverity.Info, userId, null, new { year, month }, ct);
        return report;
    }
    public Task SaveReportAsync(Guid userId, PersonalReport report, CancellationToken ct = default) => reports.CreateAsync(new UserReport(Guid.NewGuid(), userId, "personal", report.PeriodStart, report.PeriodEnd, JsonSerializer.Serialize(report), DateTime.UtcNow), ct);
    public async Task<Result<byte[]>> ExportPersonalReportCsvAsync(Guid userId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default)
    {
        try { var r = await BuildAsync(userId, periodStart, periodEnd, ct); var csv = $"Periodo inicial,Periodo final,Conclusoes,Dias ativos,Taxa,Insight\n{r.PeriodStart},{r.PeriodEnd},{r.TotalCompletions},{r.ActiveDays},{r.CompletionRate.ToString(CultureInfo.InvariantCulture)},\"{SanitizeCsv(r.Insight)}\"\n"; await audit.LogAsync("report_exported", "Relatório exportado", AuditSeverity.Info, userId, null, new { periodStart, periodEnd }, ct); return Result<byte[]>.Success(Encoding.UTF8.GetBytes(csv)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao exportar relatório de {UserId}", userId); return Result<byte[]>.Failure("report.export_error", "Não foi possível exportar o relatório."); }
    }
    private async Task<PersonalReport> BuildAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken ct) { var c = await completions.ListByUserAsync(userId, start, ct); var inPeriod = c.Where(x => x.CompletedDate <= end).ToList(); var habitCount = Math.Max(1, (await habits.ListByUserAsync(userId, ct)).Count(x => !x.IsArchived)); var days = end.DayNumber - start.DayNumber + 1; var rate = Math.Round(inPeriod.Count * 100d / (habitCount * days), 1); return new(start, end, inPeriod.Count, inPeriod.Select(x => x.CompletedDate).Distinct().Count(), rate, rate >= 70 ? "Você manteve uma ótima consistência." : "Escolha um hábito pequeno para retomar hoje."); }
    public static string SanitizeCsv(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : ("=+-@".Contains(value[0]) ? "'" + value : value).Replace("\"", "\"\"");
}
