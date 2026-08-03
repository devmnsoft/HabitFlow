using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class ReminderScheduleCalculator(TimeProvider timeProvider)
{
    public DateTimeOffset Next(TimeOnly localTime, IReadOnlyCollection<int> days, string timezone, DateTimeOffset? after = null)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timezone); }
        catch (TimeZoneNotFoundException) { throw new ArgumentException("Fuso horário inválido.", nameof(timezone)); }
        var instant = after ?? timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(instant, zone);
        for (var offset = 0; offset <= 8; offset++)
        {
            var date = DateOnly.FromDateTime(localNow.DateTime).AddDays(offset);
            var weekday = (int)date.DayOfWeek;
            if (days.Count > 0 && !days.Contains(weekday)) continue;
            var candidate = date.ToDateTime(localTime, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(candidate)) candidate = candidate.AddHours(1);
            var utc = TimeZoneInfo.ConvertTimeToUtc(candidate, zone);
            var result = new DateTimeOffset(utc, TimeSpan.Zero);
            if (result > instant) return result;
        }
        throw new InvalidOperationException("Não foi possível calcular a próxima lembrança.");
    }
}

public sealed class HabitReminderService(IHabitReminderRepository repository, ReminderScheduleCalculator schedules)
{
    public Task<IReadOnlyList<HabitReminder>> ListAsync(Guid clientId, Guid userId, Guid? habitId, CancellationToken ct = default) =>
        repository.ListAsync(clientId, userId, habitId, ct);

    public async Task<Result> CreateAsync(Guid clientId, Guid userId, Guid habitId, TimeOnly time, int[] days, string timezone, CancellationToken ct = default)
    {
        if (!await repository.HabitBelongsToUserAsync(clientId, userId, habitId, ct))
            return Result.Failure("reminder.habit_not_found", "Hábito não encontrado.");
        if (days.Length == 0 || days.Distinct().Any(x => x is < 0 or > 6))
            return Result.Failure("reminder.days", "Escolha ao menos um dia válido.");
        DateTimeOffset next;
        try { next = schedules.Next(time, days, timezone); }
        catch (ArgumentException) { return Result.Failure("reminder.timezone", "Escolha um fuso horário válido."); }
        await repository.CreateAsync(new HabitReminder(Guid.NewGuid(), clientId, userId, habitId, "", time, timezone,
            days.Distinct().Order().ToArray(), true, null, next, DateTime.UtcNow, DateTime.UtcNow), ct);
        return Result.Success();
    }

    public async Task<Result> SetActiveAsync(Guid clientId, Guid userId, Guid id, bool active, CancellationToken ct = default)
    {
        var reminder = await repository.GetOwnedAsync(clientId, userId, id, ct);
        if (reminder is null) return Result.Failure("reminder.not_found", "Lembrete não encontrado.");
        var next = active ? schedules.Next(reminder.ReminderTime, reminder.DaysOfWeek, reminder.Timezone) : null;
        return await repository.SetActiveAsync(clientId, userId, id, active, next, ct)
            ? Result.Success() : Result.Failure("reminder.conflict", "O lembrete foi alterado. Atualize a página.");
    }
}
