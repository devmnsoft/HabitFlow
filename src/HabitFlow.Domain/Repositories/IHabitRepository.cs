namespace HabitFlow.Domain;

public interface IHabitRepository
{
    Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Habit>> ListActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<Habit?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, Guid idempotencyKey, CancellationToken ct = default);
    Task<Habit?> FindActiveBySourceTemplateAsync(Guid clientId, Guid userId, Guid templateId, bool includeVariations, CancellationToken ct = default);
    Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<Habit?> GetAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task<Habit?> GetAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Habit habit, CancellationToken ct = default);
    Task UpdateAsync(Habit habit, CancellationToken ct = default);
}
