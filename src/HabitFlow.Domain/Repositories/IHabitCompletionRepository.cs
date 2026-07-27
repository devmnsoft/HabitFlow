namespace HabitFlow.Domain;

public interface IHabitCompletionRepository
{
    Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default);
    Task AddAsync(HabitCompletion completion, CancellationToken ct = default);
    Task DeleteAsync(Guid habitId, Guid userId, DateOnly date, CancellationToken ct = default);
}
