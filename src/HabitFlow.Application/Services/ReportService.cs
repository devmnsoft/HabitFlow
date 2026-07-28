using System.Globalization;
using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record PersonalReport(DateOnly PeriodStart, DateOnly PeriodEnd, int TotalCompletions, int ActiveDays, double CompletionRate, string Insight);

public sealed class ReportService(IHabitRepository habits, IHabitCompletionRepository completions, IHabitWeekDayRepository weekDays,
    HabitOccurrenceService occurrences, UserTimeZoneService timeZones, IUserReportRepository reports, AuditService audit, ILogger<ReportService> logger)
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
    private async Task<PersonalReport> BuildAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var domainHabits = await habits.ListByUserAsync(userId, ct);
        var rows = domainHabits.Select(h => new ProgressHabitRow { Id = h.Id, Name = h.Name, Category = h.Category,
            IsArchived = h.IsArchived, ArchivedAt = h.ArchivedAt, CreatedAt = h.CreatedAt, FrequencyTypeCode = h.FrequencyType.ToString(), ReminderTime = h.ReminderTime }).ToList();
        var configured = await weekDays.ListByHabitsAsync(rows.Select(x => x.Id), ct);
        var schedule = configured.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(y => y.DayOfWeek).ToHashSet());
        var today = timeZones.Today(); var historicalEnd = end > today ? today : end;
        var planned = historicalEnd < start ? [] : await occurrences.ListScheduledForPeriodAsync(rows, schedule, start, historicalEnd, timeZones.Resolve());
        var valid = planned.Select(x => (x.Habit.Id, x.Date)).ToHashSet();
        var completed = (await completions.ListByUserAsync(userId, start, ct)).Where(x => x.CompletedDate <= historicalEnd)
            .Select(x => (x.HabitId, x.CompletedDate)).Distinct().Count(valid.Contains);
        var rate = valid.Count == 0 ? 0 : Math.Round(completed * 100d / valid.Count, 1);
        return new(start, end, completed, valid.Select(x => x.Date).Distinct().Count(), rate,
            rate >= 70 ? "Você manteve uma ótima consistência." : "Escolha um hábito pequeno para retomar hoje.");
    }
    public static string SanitizeCsv(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : ("=+-@".Contains(value[0]) ? "'" + value : value).Replace("\"", "\"\"");
}
