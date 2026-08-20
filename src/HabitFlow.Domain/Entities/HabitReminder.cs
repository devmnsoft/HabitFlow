namespace HabitFlow.Domain;

public sealed record HabitReminder(Guid Id, Guid ClientId, Guid UserId, Guid HabitId, string HabitName,
    TimeOnly ReminderTime, string Timezone, int[] DaysOfWeek, bool IsActive,
    DateTimeOffset? LastTriggeredAt, DateTimeOffset? NextTriggerAt, DateTime CreatedAt, DateTime UpdatedAt);

public interface IHabitReminderRepository
{
    Task<IReadOnlyList<HabitReminder>> ListAsync(Guid clientId, Guid userId, Guid? habitId = null, CancellationToken ct = default);
    Task<int> CountForHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task<HabitReminder?> GetOwnedAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> HabitBelongsToUserAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task CreateAsync(HabitReminder reminder, CancellationToken ct = default);
    Task<bool> SetActiveAsync(Guid clientId, Guid userId, Guid id, bool active, DateTimeOffset? next, CancellationToken ct = default);
    Task<bool> SnoozeAsync(Guid clientId, Guid userId, Guid id, DateTimeOffset next, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
}
