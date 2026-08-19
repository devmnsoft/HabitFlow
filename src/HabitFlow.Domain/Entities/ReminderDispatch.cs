namespace HabitFlow.Domain;

public sealed record ReminderDispatchCandidate(
    Guid DispatchId, Guid ReminderId, Guid ClientId, Guid UserId, Guid HabitId,
    string HabitName, TimeOnly ReminderTime, string Timezone, int[] DaysOfWeek,
    DateTimeOffset ScheduledFor, int AttemptCount, Guid CorrelationId);

public sealed record ReminderDispatchResult(int Claimed, int Delivered, int Retried, int Failed);

public sealed record ReminderDispatchHealth(
    long Due, long Pending, long Retries, long Failed);

public interface IReminderDispatchRepository
{
    Task<IReadOnlyList<ReminderDispatchCandidate>> ClaimAsync(DateTimeOffset now, int batchSize,
        string workerId, TimeSpan lease, CancellationToken ct = default);
    Task CompleteAsync(ReminderDispatchCandidate candidate, DateTimeOffset nextOccurrence,
        DateTimeOffset now, CancellationToken ct = default);
    Task<bool> FailAsync(ReminderDispatchCandidate candidate, string errorCode, DateTimeOffset now,
        DateTimeOffset? nextAttempt, CancellationToken ct = default);
    Task<ReminderDispatchHealth> GetHealthAsync(DateTimeOffset now, CancellationToken ct = default);
}
