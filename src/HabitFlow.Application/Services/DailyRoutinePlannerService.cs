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

public sealed class DailyRoutinePlannerService(IHabitRepository habits, IHabitWeekDayRepository weekDays, IHabitCompletionRepository completions, IHabitScheduleExceptionRepository exceptions, IDailyRoutineOverrideRepository overrides, EffectiveHabitScheduleService schedule, TimeProvider clock, UserTimeZoneService timeZone)
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
        var effective = await schedule.BuildAsync(new(query.ClientId, query.UserId, query.LocalDate, query.LocalDate, rows, dayMap, exceptionList, overrideMap.Values.ToList(), timeZone.Resolve()));
        var now = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone.Resolve()).DateTime);
        var byHabit = source.ToDictionary(x => x.Id);
        var items = effective.Occurrences.Where(x => byHabit.ContainsKey(x.Habit.Id)).Select(occurrence =>
        {
            var h = byHabit[occurrence.Habit.Id];
            overrideMap.TryGetValue(h.Id, out var custom);
            var preferred = occurrence.EffectiveTime;
            var status = occurrence.Status switch
            {
                EffectiveOccurrenceStatus.Excused => DailyRoutineItemStatus.Excused,
                EffectiveOccurrenceStatus.MovedOut => DailyRoutineItemStatus.Moved,
                _ when completionSet.Contains(h.Id) => DailyRoutineItemStatus.Completed,
                _ when preferred.HasValue && preferred > now => DailyRoutineItemStatus.Upcoming,
                _ => DailyRoutineItemStatus.Available
            };
            var action = status switch { DailyRoutineItemStatus.Completed => "Desfazer", DailyRoutineItemStatus.Excused or DailyRoutineItemStatus.Moved => "Restaurar", _ => "Concluir" };
            return new DailyRoutineItem(h.Id,h.Name,h.Color,h.Category,preferred,h.EstimatedTimeMinutes ?? 10,status,custom?.SortOrder ?? h.SortOrder,occurrence.ExceptionVersion > 0 ? occurrence.ExceptionVersion : custom?.Version ?? 0,action);
        }).OrderBy(x => x.PreferredTime.HasValue ? 0 : 1).ThenBy(x => x.PreferredTime).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
        var done = items.Count(x => x.Status == DailyRoutineItemStatus.Completed);
        var scheduled = effective.EffectiveOccurrences.Count;
        return new(query.LocalDate,items,scheduled,done,scheduled == 0 ? 0 : (int)Math.Round(done * 100d / scheduled));
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
        var mutation = await exceptions.UpsertAsync(new(Guid.NewGuid(),clientId,userId,habitId,date,type,destination,reason?.Trim(),1,now,now),0,ct);
        return mutation.Succeeded ? Result.Success() : Result.Failure("schedule.conflict","A rotina foi alterada em outra sessão. Atualize a página e tente novamente.");
    }

    public async Task<Result> RestoreOriginalScheduleAsync(Guid clientId, Guid userId, Guid habitId, DateOnly date, int expectedVersion, CancellationToken ct=default)
    {
        if (await habits.GetAsync(clientId,userId,habitId,ct) is null) return Result.Failure("habit.not_found","Hábito não encontrado.");
        var mutation = await exceptions.DeleteAsync(clientId,userId,habitId,date,expectedVersion,ct);
        return mutation.Succeeded ? Result.Success() : Result.Failure("schedule.conflict","A rotina foi alterada em outra sessão. Atualize a página e tente novamente.");
    }
}
