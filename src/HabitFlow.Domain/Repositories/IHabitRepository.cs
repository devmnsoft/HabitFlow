namespace HabitFlow.Domain;

public interface IHabitRepository
{
    Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Habit?> GetAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Habit habit, CancellationToken ct = default);
    Task UpdateAsync(Habit habit, CancellationToken ct = default);
}
