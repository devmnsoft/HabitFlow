using HabitFlow.Domain;

namespace HabitFlow.Application;

public enum EffectiveOccurrenceStatus { Scheduled, Added, MovedIn, MovedOut, Excused }

public sealed record EffectiveHabitScheduleQuery(
    Guid ClientId,
    Guid UserId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ProgressHabitRow> Habits,
    IReadOnlyDictionary<Guid, IReadOnlySet<int>> WeekDays,
    IReadOnlyList<HabitScheduleException> Exceptions,
    IReadOnlyList<DailyRoutineOverride> Overrides,
    TimeZoneInfo TimeZone);

public sealed record EffectiveHabitOccurrence(
    ProgressHabitRow Habit,
    DateOnly Date,
    TimeOnly? EffectiveTime,
    EffectiveOccurrenceStatus Status,
    DateOnly? OriginDate = null,
    int ExceptionVersion = 0)
{
    public bool IsEffective => Status is EffectiveOccurrenceStatus.Scheduled or EffectiveOccurrenceStatus.Added or EffectiveOccurrenceStatus.MovedIn;
}

public sealed record EffectiveScheduleResult(IReadOnlyList<EffectiveHabitOccurrence> Occurrences)
{
    public IReadOnlyList<EffectiveHabitOccurrence> EffectiveOccurrences => Occurrences.Where(x => x.IsEffective).ToList();
}

/// <summary>The single authority that combines recurrence, exceptions and per-day overrides.</summary>
public sealed class EffectiveHabitScheduleService(HabitOccurrenceService baseSchedule)
{
    public async Task<EffectiveScheduleResult> BuildAsync(EffectiveHabitScheduleQuery query)
    {
        if (query.ClientId == Guid.Empty || query.UserId == Guid.Empty)
            throw new ArgumentException("Conta e pessoa são obrigatórias.");
        if (query.To < query.From)
            throw new ArgumentException("O fim do período deve ser igual ou posterior ao início.");

        var habits = query.Habits.ToDictionary(x => x.Id);
        var times = query.Overrides
            .Where(x => x.ClientId == query.ClientId && x.UserId == query.UserId)
            .ToDictionary(x => (x.HabitId, x.LocalDate), x => x.PreferredTime);
        var result = (await baseSchedule.ListScheduledForPeriodAsync(
                query.Habits, query.WeekDays, query.From, query.To, query.TimeZone))
            .Select(x => new EffectiveHabitOccurrence(
                x.Habit, x.Date, times.GetValueOrDefault((x.Habit.Id, x.Date)) ?? x.Habit.ReminderTime,
                EffectiveOccurrenceStatus.Scheduled))
            .ToDictionary(x => (x.Habit.Id, x.Date));

        foreach (var exception in query.Exceptions.Where(x => x.ClientId == query.ClientId && x.UserId == query.UserId))
        {
            if (!habits.TryGetValue(exception.HabitId, out var habit)) continue;
            var originKey = (exception.HabitId, exception.LocalDate);
            if (exception.LocalDate >= query.From && exception.LocalDate <= query.To &&
                exception.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved)
            {
                result[originKey] = new(habit, exception.LocalDate,
                    times.GetValueOrDefault(originKey) ?? habit.ReminderTime,
                    exception.Type == HabitScheduleExceptionType.Excused
                        ? EffectiveOccurrenceStatus.Excused : EffectiveOccurrenceStatus.MovedOut,
                    ExceptionVersion: exception.Version);
            }

            var destination = exception.Type == HabitScheduleExceptionType.Moved
                ? exception.DestinationDate : exception.Type == HabitScheduleExceptionType.Added ? exception.LocalDate : null;
            if (destination is not { } date || date < query.From || date > query.To) continue;
            var destinationKey = (exception.HabitId, date);
            result[destinationKey] = new(habit, date,
                times.GetValueOrDefault(destinationKey) ?? habit.ReminderTime,
                exception.Type == HabitScheduleExceptionType.Moved
                    ? EffectiveOccurrenceStatus.MovedIn : EffectiveOccurrenceStatus.Added,
                exception.Type == HabitScheduleExceptionType.Moved ? exception.LocalDate : null,
                exception.Version);
        }

        return new(result.Values.OrderBy(x => x.Date).ThenBy(x => x.EffectiveTime).ThenBy(x => x.Habit.Name).ToList());
    }
}
