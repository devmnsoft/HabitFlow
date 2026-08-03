using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public enum DailyRoutineItemStatus { Upcoming, Available, Completed, Excused, Moved, Missed }
public sealed record DailyRoutineQuery(Guid ClientId, Guid UserId, DateOnly LocalDate);
public sealed record DailyRoutineItem(Guid HabitId, string Name, string Color, string? Category, TimeOnly? PreferredTime, int EstimatedMinutes, DailyRoutineItemStatus Status, int SortOrder, int Version, string NextAction);
public sealed record DailyRoutinePlan(DateOnly LocalDate, IReadOnlyList<DailyRoutineItem> Items, int Scheduled, int Completed, int Percentage)
{
    public int Pending => Scheduled - Completed;
}

public sealed class DailyRoutinePlannerService(IHabitRepository habits, IHabitWeekDayRepository weekDays, IHabitCompletionRepository completions, IHabitScheduleExceptionRepository exceptions, IDailyRoutineOverrideRepository overrides, HabitOccurrenceService occurrences, TimeProvider clock, UserTimeZoneService timeZone)
{
    public async Task<DailyRoutinePlan> BuildAsync(DailyRoutineQuery query, CancellationToken ct = default)
    {
        if (query.ClientId == Guid.Empty || query.UserId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var source = await habits.ListActiveAsync(query.ClientId, query.UserId, ct);
        var days = await weekDays.ListByHabitsAsync(source.Select(x => x.Id), ct);
        var completionSet = (await completions.ListAsync(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, ct)).Select(x => x.HabitId).ToHashSet();
        var exceptionList = await exceptions.ListAsync(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, ct);
        var overrideMap = (await overrides.ListAsync(query.ClientId, query.UserId, query.LocalDate, ct)).ToDictionary(x => x.HabitId);
        var rows = source.Select(ToProgressRow).ToList();
        var dayMap = days.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(d => d.DayOfWeek).ToHashSet());
        var scheduled = (await occurrences.ListScheduledForDateAsync(rows, dayMap, query.LocalDate, timeZone.Resolve())).Select(x => x.Habit.Id).ToHashSet();
        foreach (var moved in exceptionList.Where(x => (x.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved) && x.LocalDate == query.LocalDate)) scheduled.Remove(moved.HabitId);
        foreach (var added in exceptionList.Where(x => x.Type == HabitScheduleExceptionType.Added || x.Type == HabitScheduleExceptionType.Moved && x.DestinationDate == query.LocalDate)) scheduled.Add(added.HabitId);
        var now = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone.Resolve()).DateTime);
        var items = source.Where(x => scheduled.Contains(x.Id)).Select(h =>
        {
            overrideMap.TryGetValue(h.Id, out var custom);
            var preferred = custom?.PreferredTime ?? h.ReminderTime;
            var status = completionSet.Contains(h.Id) ? DailyRoutineItemStatus.Completed : preferred.HasValue && preferred > now ? DailyRoutineItemStatus.Upcoming : DailyRoutineItemStatus.Available;
            return new DailyRoutineItem(h.Id,h.Name,h.Color,h.Category,preferred,h.EstimatedTimeMinutes ?? 10,status,custom?.SortOrder ?? h.SortOrder,custom?.Version ?? 0,status == DailyRoutineItemStatus.Completed ? "Desfazer" : "Concluir");
        }).OrderBy(x => x.PreferredTime.HasValue ? 0 : 1).ThenBy(x => x.PreferredTime).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
        var done = items.Count(x => x.Status == DailyRoutineItemStatus.Completed);
        return new(query.LocalDate,items,items.Count,done,items.Count == 0 ? 0 : (int)Math.Round(done * 100d / items.Count));
    }

    private static ProgressHabitRow ToProgressRow(Habit h) => new() { Id=h.Id,Name=h.Name,Category=h.Category,IsArchived=h.IsArchived,ArchivedAt=h.ArchivedAt,CreatedAt=h.StartDate?.ToDateTime(TimeOnly.MinValue) ?? h.CreatedAt,FrequencyTypeCode=h.FrequencyType.ToString(),ReminderTime=h.ReminderTime };
}

public sealed class HabitScheduleExceptionService(IHabitRepository habits, IHabitScheduleExceptionRepository exceptions, TimeProvider clock)
{
    public async Task<Result> SetAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, HabitScheduleExceptionType type, DateOnly? destination, string? reason, CancellationToken ct=default)
    {
        if (await habits.GetAsync(clientId,userId,habitId,ct) is null) return Result.Failure("habit.not_found","Hábito não encontrado.");
        if (type == HabitScheduleExceptionType.Moved && (!destination.HasValue || destination <= date)) return Result.Failure("schedule.invalid_destination","Escolha uma data futura.");
        var now=clock.GetUtcNow();
        await exceptions.UpsertAsync(new(Guid.NewGuid(),clientId,userId,habitId,date,type,destination,reason?.Trim(),1,now,now),ct);
        return Result.Success();
    }
}
