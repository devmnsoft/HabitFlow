namespace HabitFlow.Domain;

public interface IHabitWeekDayRepository
{
    Task<IReadOnlyList<HabitWeekDay>> ListByHabitAsync(Guid habitId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<HabitWeekDay>>> ListByHabitsAsync(IEnumerable<Guid> habitIds, CancellationToken ct = default);
    Task ReplaceAsync(Guid habitId, IReadOnlyCollection<int> days, CancellationToken ct = default);
}
