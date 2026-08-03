namespace HabitFlow.Domain;

public interface IHabitCompletionRepository
{
    Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default);
    Task<IReadOnlyList<HabitCompletion>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<CompletionMutationResult> AddIfMissingAsync(Guid clientId, Guid userId, Guid habitId,
        DateOnly localDate, Guid completionId, CancellationToken ct = default);
    Task<CompletionMutationResult> DeleteIfExistsAsync(Guid clientId, Guid userId, Guid habitId,
        DateOnly localDate, CancellationToken ct = default);

    [Obsolete("Use AddIfMissingAsync so callers can distinguish an idempotent replay.")]
    Task AddAsync(HabitCompletion completion, CancellationToken ct = default);
    [Obsolete("Use DeleteIfExistsAsync so callers can distinguish an idempotent replay.")]
    Task DeleteAsync(Guid habitId, Guid userId, DateOnly date, CancellationToken ct = default);
}

public sealed record CompletionMutationResult(
    Guid? CompletionId,
    bool Created,
    bool Deleted,
    bool FinalState,
    DateOnly LocalDate);
