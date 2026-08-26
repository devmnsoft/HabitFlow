using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record AnalyticsPeriod(DateOnly Start, DateOnly End)
{
    public static AnalyticsPeriod Create(DateOnly start, DateOnly end, DateOnly today)
    {
        if (end < start) throw new ArgumentException("A data final deve ser igual ou posterior à data inicial.");
        if (end > today.AddDays(1)) throw new ArgumentException("O período não pode terminar no futuro.");
        if (end.DayNumber - start.DayNumber > 366) throw new ArgumentException("Selecione um período de até 366 dias.");
        return new(start, end);
    }
}

public sealed record AnalyticsFilter(AnalyticsPeriod Period, Guid? HabitId = null, string? Category = null,
    string? Status = null, HabitFrequencyType? Frequency = null, bool? HasReminder = null,
    bool? HasLinkedGoal = null, bool? Completed = null);

public sealed record AnalyticsDay(DateOnly Date, int Completions);
public sealed record AnalyticsCategory(string Name, int Completions);
public sealed record AnalyticsHabit(Guid Id, string Name, string? Category, string Status, int Completions, double Consistency,
    int CurrentStreak, int BestStreak, bool HasReminder);
public sealed record AnalyticsDashboard(
    AnalyticsFilter Filter, int ActiveHabits, int PausedHabits, int ArchivedHabits, int CompletionsToday,
    int CompletionsWeek, int CompletionsMonth, double Consistency7, double Consistency28, double Consistency90,
    int CurrentStreak, int BestStreak, int ActiveReminders, int LateReminders,
    IReadOnlyList<AnalyticsDay> Daily, IReadOnlyList<AnalyticsCategory> Categories,
    IReadOnlyList<AnalyticsHabit> Habits, IReadOnlyList<HabitInsight> Insights, IReadOnlyList<Habit> FilterHabits,
    IReadOnlyList<string> FilterCategories);

public interface IAnalyticsQueryService
{
    Task<AnalyticsDashboard> GetMyEvolutionAsync(Guid clientId, Guid userId, AnalyticsFilter filter, CancellationToken ct = default);
}

/// <summary>Builds tenant-scoped analytics exclusively from persisted habits, completions and reminders.</summary>
public sealed class AnalyticsQueryService(IHabitRepository habitRepository, IHabitCompletionRepository completionRepository,
    IHabitReminderRepository reminderRepository, TimeProvider clock) : IAnalyticsQueryService
{
    public async Task<AnalyticsDashboard> GetMyEvolutionAsync(Guid clientId, Guid userId, AnalyticsFilter filter, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        AnalyticsPeriod.Create(filter.Period.Start, filter.Period.End, today);
        var allHabits = await habitRepository.ListAsync(clientId, userId, ct);
        var reminders = await reminderRepository.ListAsync(clientId, userId, null, ct);
        var reminderHabitIds = reminders.Where(x => x.IsActive).Select(x => x.HabitId).ToHashSet();
        var selected = allHabits.Where(h => (!filter.HabitId.HasValue || h.Id == filter.HabitId)
            && (string.IsNullOrWhiteSpace(filter.Category) || string.Equals(h.Category, filter.Category, StringComparison.OrdinalIgnoreCase))
            && (filter.Frequency is null || h.FrequencyType == filter.Frequency)
            && (filter.HasReminder is null || reminderHabitIds.Contains(h.Id) == filter.HasReminder)
            && (string.IsNullOrWhiteSpace(filter.Status) || Status(h).Equals(filter.Status, StringComparison.OrdinalIgnoreCase))).ToList();
        var earliest = new[] { filter.Period.Start, today.AddDays(-89), new DateOnly(today.Year, today.Month, 1), today.AddDays(-6) }.Min();
        var completions = await completionRepository.ListAsync(clientId, userId, earliest, today, ct);
        var selectedIds = selected.Select(x => x.Id).ToHashSet();
        var scoped = completions.Where(x => selectedIds.Contains(x.HabitId)).ToList();
        if (filter.Completed.HasValue)
        {
            var completedIds = scoped.Select(x => x.HabitId).ToHashSet();
            selected = selected.Where(x => completedIds.Contains(x.Id) == filter.Completed).ToList();
            selectedIds = selected.Select(x => x.Id).ToHashSet(); scoped = scoped.Where(x => selectedIds.Contains(x.HabitId)).ToList();
        }
        double Rate(int days) => ConsistencyPercentage(selected, scoped, today.AddDays(1-days), today);
        var habitRows = selected.Select(h => BuildHabit(h, scoped.Where(x => x.HabitId == h.Id).Select(x => x.CompletedDate), reminderHabitIds.Contains(h.Id), filter.Period)).OrderByDescending(x => x.Consistency).ThenBy(x => x.Name).ToList();
        var daily = scoped.Where(x => x.CompletedDate >= filter.Period.Start && x.CompletedDate <= filter.Period.End)
            .GroupBy(x => x.CompletedDate).ToDictionary(x => x.Key, x => x.Select(c => c.HabitId).Distinct().Count());
        var insights = BuildInsights(clientId, userId, habitRows, clock.GetUtcNow()).Take(5).ToList();
        return new(filter, allHabits.Count(h => !h.IsArchived && !h.IsPaused), allHabits.Count(h => h.IsPaused && !h.IsArchived), allHabits.Count(h => h.IsArchived),
            scoped.Count(x => x.CompletedDate == today), scoped.Count(x => x.CompletedDate >= today.AddDays(-6)), scoped.Count(x => x.CompletedDate >= new DateOnly(today.Year,today.Month,1)),
            Rate(7), Rate(28), Rate(90), habitRows.Count == 0 ? 0 : habitRows.Max(x => x.CurrentStreak), habitRows.Count == 0 ? 0 : habitRows.Max(x => x.BestStreak),
            reminders.Count(x => x.IsActive), reminders.Count(x => x.IsActive && x.NextTriggerAt < clock.GetUtcNow()),
            Enumerable.Range(0, filter.Period.End.DayNumber-filter.Period.Start.DayNumber+1).Select(i => new AnalyticsDay(filter.Period.Start.AddDays(i), daily.GetValueOrDefault(filter.Period.Start.AddDays(i)))).ToList(),
            selected.GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Sem categoria" : x.Category!).Select(g => new AnalyticsCategory(g.Key, scoped.Count(c => g.Any(h => h.Id == c.HabitId)))).OrderByDescending(x => x.Completions).ToList(),
            habitRows, insights, allHabits.OrderBy(x => x.Name).ToList(), allHabits.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().Cast<string>().ToList());
    }

    public static double ConsistencyPercentage(IEnumerable<Habit> habits, IEnumerable<HabitCompletion> completions, DateOnly start, DateOnly end)
    {
        var list = habits.Where(h => !h.IsArchived && !h.IsPaused).ToList();
        var expected = list.Sum(h => Math.Max(0, end.DayNumber - Max(start, DateOnly.FromDateTime(h.CreatedAt)).DayNumber + 1));
        if (expected == 0) return 0;
        var ids = list.Select(x => x.Id).ToHashSet();
        var done = completions.Where(c => ids.Contains(c.HabitId) && c.CompletedDate >= start && c.CompletedDate <= end).Select(c => (c.HabitId,c.CompletedDate)).Distinct().Count();
        return Math.Round(done * 100d / expected, 1);
    }
    private static AnalyticsHabit BuildHabit(Habit h, IEnumerable<DateOnly> dates, bool reminder, AnalyticsPeriod period)
    {
        var set=dates.Distinct().ToHashSet(); var run=0; var best=0; var current=0;
        for(var d=period.Start;d<=period.End;d=d.AddDays(1)){if(set.Contains(d)){run++;best=Math.Max(best,run);}else run=0;}
        for(var d=period.End;d>=period.Start&&set.Contains(d);d=d.AddDays(-1)) current++;
        var eligible=Math.Max(0,period.End.DayNumber-Max(period.Start,DateOnly.FromDateTime(h.CreatedAt)).DayNumber+1);
        return new(h.Id,h.Name,h.Category,Status(h),set.Count(x=>x>=period.Start&&x<=period.End),eligible==0?0:Math.Round(set.Count(x=>x>=period.Start&&x<=period.End)*100d/eligible,1),current,best,reminder);
    }
    private static IReadOnlyList<HabitInsight> BuildInsights(Guid client, Guid user, IReadOnlyList<AnalyticsHabit> habits, DateTimeOffset now)
    {
        var result=new List<HabitInsight>();
        foreach(var h in habits.Where(x=>x.Consistency<40).Take(3)) result.Add(new(HabitInsightType.ConsistencyDropped,"Um ritmo merece atenção",$"{h.Name} teve menos registros nos últimos dias. Um lembrete leve pode ajudar.", $"Consistência calculada em {h.Consistency}% no período.",h.HasReminder?"Revisar a rotina":"Configurar lembrete",h.HasReminder?"/habits":"/reminders",HabitInsightSeverity.Attention,h.Category??"Sem categoria",now,client,user));
        return result;
    }
    private static string Status(Habit h)=>h.IsArchived?"Arquivado":h.IsPaused?"Pausado":"Ativo";
    private static DateOnly Max(DateOnly a,DateOnly b)=>a>b?a:b;
}
