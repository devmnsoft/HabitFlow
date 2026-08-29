using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record AnalyticsPeriod(DateOnly Start, DateOnly End)
{
    public static AnalyticsPeriod Create(DateOnly start, DateOnly end, DateOnly today)
    {
        if (end < start) throw new ArgumentException("A data final deve ser igual ou posterior à data inicial.");
        if (end > today) throw new ArgumentException("O período não pode terminar no futuro.");
        if (end.DayNumber - start.DayNumber > 366) throw new ArgumentException("Selecione um período de até 366 dias.");
        return new(start, end);
    }
}

public sealed record AnalyticsFilter(AnalyticsPeriod Period, Guid? HabitId = null, string? Category = null,
    string? Status = null, HabitFrequencyType? Frequency = null, bool? HasReminder = null,
    bool? HasLinkedGoal = null, bool? Completed = null);
public sealed record AnalyticsDay(DateOnly Date, int Scheduled, int Completions, double CompletionRate);
public sealed record AnalyticsCategory(string Name, int Scheduled, int Completions, double CompletionRate);
public sealed record AnalyticsWeekDay(DayOfWeek Day, int Scheduled, int Completions, double CompletionRate);
public sealed record AnalyticsHabit(Guid Id, string Name, string? Category, string Status, int Scheduled, int Completions,
    int MissedCount, double Consistency, int CurrentStreak, int BestStreak, bool HasReminder);
public sealed record AnalyticsDashboard(
    AnalyticsFilter Filter, int ActiveHabits, int PausedHabits, int ArchivedHabits, int CompletionsToday,
    int CompletionsWeek, int CompletionsMonth, double CompletionRateDaily, double CompletionRateWeekly,
    double CompletionRateMonthly, double Consistency7, double Consistency28, double Consistency90,
    int CurrentStreak, int BestStreak, int MissedCount, int ActiveReminders, int LateReminders,
    IReadOnlyList<AnalyticsDay> Daily, IReadOnlyList<AnalyticsCategory> Categories,
    IReadOnlyList<AnalyticsWeekDay> WeekDays, IReadOnlyList<AnalyticsHabit> Habits,
    IReadOnlyList<HabitInsight> Insights, IReadOnlyList<Habit> FilterHabits, IReadOnlyList<string> FilterCategories,
    string? BestTimeOfDay);

public interface IAnalyticsQueryService
{
    Task<AnalyticsDashboard> GetMyEvolutionAsync(Guid clientId, Guid userId, AnalyticsFilter filter, CancellationToken ct = default);
}

/// <summary>Builds tenant-scoped analytics exclusively from persisted schedules and unique completions.</summary>
public sealed class AnalyticsQueryService(IHabitRepository habitRepository, IHabitCompletionRepository completionRepository,
    IHabitReminderRepository reminderRepository, IHabitWeekDayRepository weekDayRepository,
    MetricCalculationService metricCalculator, UserTimeZoneService timeZones, TimeProvider clock) : IAnalyticsQueryService
{
    public async Task<AnalyticsDashboard> GetMyEvolutionAsync(Guid clientId, Guid userId, AnalyticsFilter filter, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var today = timeZones.Today();
        AnalyticsPeriod.Create(filter.Period.Start, filter.Period.End, today);
        var allHabits = await habitRepository.ListAsync(clientId, userId, ct);
        var reminders = await reminderRepository.ListAsync(clientId, userId, null, ct);
        var reminderHabitIds = reminders.Where(x => x.IsActive).Select(x => x.HabitId).ToHashSet();
        var selected = allHabits.Where(h => (!filter.HabitId.HasValue || h.Id == filter.HabitId)
            && (string.IsNullOrWhiteSpace(filter.Category) || string.Equals(h.Category, filter.Category, StringComparison.OrdinalIgnoreCase))
            && (filter.Frequency is null || h.FrequencyType == filter.Frequency)
            && (filter.HasReminder is null || reminderHabitIds.Contains(h.Id) == filter.HasReminder)
            && (string.IsNullOrWhiteSpace(filter.Status) || Status(h).Equals(filter.Status, StringComparison.OrdinalIgnoreCase))).ToList();
        var weekDays = await weekDayRepository.ListByHabitsAsync(selected.Select(x => x.Id), ct);
        var schedule = selected.ToDictionary(x => x.Id, x => (IReadOnlySet<int>)(weekDays.TryGetValue(x.Id, out var days)
            ? days.Select(y => y.DayOfWeek).ToHashSet() : new HashSet<int>()));
        var earliest = new[] { filter.Period.Start, today.AddDays(-89), new DateOnly(today.Year, today.Month, 1), today.AddDays(-6) }.Min();
        var completions = (await completionRepository.ListAsync(clientId, userId, earliest, today, ct))
            .GroupBy(x => (x.HabitId, x.CompletedDate)).Select(x => x.OrderBy(y => y.CreatedAt).First()).ToList();
        if (filter.Completed.HasValue)
        {
            var completedIds = completions.Select(x => x.HabitId).ToHashSet();
            selected = selected.Where(x => completedIds.Contains(x.Id) == filter.Completed).ToList();
        }
        var selectedIds = selected.Select(x => x.Id).ToHashSet();
        completions = completions.Where(x => selectedIds.Contains(x.HabitId)).ToList();
        var completedKeys = completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        List<MetricOccurrence> Occurrences(DateOnly start, DateOnly end) => selected
            .SelectMany(h => Enumerable.Range(0, Math.Max(0, end.DayNumber - start.DayNumber + 1))
                .Select(i => start.AddDays(i)).Where(d => MetricCalculationService.IsScheduled(h, schedule[h.Id], d))
                .Select(d => new MetricOccurrence(h.Id, d, completedKeys.Contains((h.Id, d))))).ToList();
        CalculatedMetrics Metrics(DateOnly start, DateOnly end) => metricCalculator.Calculate(Occurrences(start, end), today);
        var periodOccurrences = Occurrences(filter.Period.Start, filter.Period.End);
        var periodMetrics = metricCalculator.Calculate(periodOccurrences, today);
        var daily = periodOccurrences.GroupBy(x => x.Date).OrderBy(x => x.Key).Select(x => new AnalyticsDay(x.Key, x.Count(), x.Count(y => y.Completed), Rate(x.Count(y => y.Completed), x.Count()))).ToList();
        var habits = selected.Select(h =>
        {
            var rows = periodOccurrences.Where(x => x.HabitId == h.Id).ToList(); var metric = metricCalculator.Calculate(rows, today);
            return new AnalyticsHabit(h.Id, h.Name, h.Category, Status(h), rows.Count, rows.Count(x => x.Completed), metric.MissedCount,
                metric.ConsistencyScore, metric.CurrentStreak, metric.BestStreak, reminderHabitIds.Contains(h.Id));
        }).OrderByDescending(x => x.Consistency).ThenBy(x => x.Name).ToList();
        var categories = selected.GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Sem categoria" : x.Category!).Select(g =>
        {
            var ids = g.Select(x => x.Id).ToHashSet(); var rows = periodOccurrences.Where(x => ids.Contains(x.HabitId)).ToList();
            return new AnalyticsCategory(g.Key, rows.Count, rows.Count(x => x.Completed), Rate(rows.Count(x => x.Completed), rows.Count));
        }).OrderByDescending(x => x.CompletionRate).ToList();
        var dayRows = periodOccurrences.GroupBy(x => x.Date.DayOfWeek).Select(x => new AnalyticsWeekDay(x.Key, x.Count(), x.Count(y => y.Completed), Rate(x.Count(y => y.Completed), x.Count()))).OrderBy(x => (int)x.Day).ToList();
        var m7 = Metrics(today.AddDays(-6), today); var m28 = Metrics(today.AddDays(-27), today); var m90 = Metrics(today.AddDays(-89), today);
        var month = Metrics(new DateOnly(today.Year, today.Month, 1), today); var todayMetric = Metrics(today, today);
        var localHours = completions.Select(x => LocalHour(x.CreatedAt, timeZones.Resolve())).ToList();
        var bestTime = localHours.Count < 3 ? null : TimeLabel(localHours.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key);
        var insights = BuildInsights(clientId, userId, habits, dayRows, bestTime, clock.GetUtcNow()).Take(5).ToList();
        return new(filter, allHabits.Count(h => !h.IsArchived && !h.IsPaused), allHabits.Count(h => h.IsPaused && !h.IsArchived), allHabits.Count(h => h.IsArchived),
            todayMetric is { } ? Occurrences(today, today).Count(x => x.Completed) : 0, Occurrences(today.AddDays(-6), today).Count(x => x.Completed), Occurrences(new(today.Year, today.Month, 1), today).Count(x => x.Completed),
            todayMetric.CompletionRate, m7.CompletionRate, month.CompletionRate, m7.ConsistencyScore, m28.ConsistencyScore, m90.ConsistencyScore,
            periodMetrics.CurrentStreak, periodMetrics.BestStreak, periodMetrics.MissedCount, reminders.Count(x => x.IsActive), reminders.Count(x => x.IsActive && x.NextTriggerAt < clock.GetUtcNow()),
            daily, categories, dayRows, habits, insights, allHabits.OrderBy(x => x.Name).ToList(), allHabits.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().Cast<string>().ToList(), bestTime);
    }

    public static double ConsistencyPercentage(IEnumerable<Habit> habits, IEnumerable<HabitCompletion> completions, DateOnly start, DateOnly end)
    {
        var rows = habits.Where(h => !h.IsArchived && !h.IsPaused).SelectMany(h => Enumerable.Range(0, Math.Max(0, end.DayNumber-start.DayNumber+1)).Select(i => start.AddDays(i))
            .Where(d => MetricCalculationService.IsScheduled(h, new HashSet<int>(), d)).Select(d => new MetricOccurrence(h.Id,d,completions.Any(c=>c.HabitId==h.Id&&c.CompletedDate==d))));
        return new MetricCalculationService().Calculate(rows,end).CompletionRate;
    }
    private static IReadOnlyList<HabitInsight> BuildInsights(Guid client, Guid user, IReadOnlyList<AnalyticsHabit> habits, IReadOnlyList<AnalyticsWeekDay> days, string? bestTime, DateTimeOffset now)
    {
        var result=new List<HabitInsight>();
        var bestDay=days.Where(x=>x.Scheduled>=2).OrderByDescending(x=>x.CompletionRate).FirstOrDefault();
        if(bestDay is not null) result.Add(new(HabitInsightType.BestHabit,"Seu dia mais consistente",$"{DayLabel(bestDay.Day)} apresenta sua maior taxa de conclusão.",$"Taxa calculada em {bestDay.CompletionRate:N1}% no período.","Ver meus hábitos","/habits",HabitInsightSeverity.Positive,"Agenda",now,client,user));
        if(bestTime is not null) result.Add(new(HabitInsightType.BestHabit,"Um horário que funciona",$"Você registra mais conclusões {bestTime}.","Baseado no horário de pelo menos três registros.","Ver minha rotina","/routines",HabitInsightSeverity.Positive,"Horários",now,client,user));
        foreach(var h in habits.Where(x=>x.Scheduled>=3&&x.Consistency<40).Take(2)) result.Add(new(HabitInsightType.ConsistencyDropped,"Um ritmo merece atenção",$"{h.Name} teve menos registros no período. Um ajuste leve pode ajudar.",$"Consistência calculada em {h.Consistency}% ({h.Completions} de {h.Scheduled}).",h.HasReminder?"Revisar a rotina":"Configurar lembrete",h.HasReminder?"/habits":"/reminders",HabitInsightSeverity.Attention,h.Category??"Sem categoria",now,client,user));
        return result;
    }
    private static int LocalHour(DateTime value, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), zone).Hour;
    private static string TimeLabel(int hour) => hour < 12 ? "pela manhã" : hour < 18 ? "à tarde" : "à noite";
    private static string DayLabel(DayOfWeek day) => new[]{"Domingo","Segunda-feira","Terça-feira","Quarta-feira","Quinta-feira","Sexta-feira","Sábado"}[(int)day];
    private static double Rate(int completed,int scheduled)=>scheduled==0?0:Math.Round(completed*100d/scheduled,1);
    private static string Status(Habit h)=>h.IsArchived?"Arquivado":h.IsPaused?"Pausado":"Ativo";
}
