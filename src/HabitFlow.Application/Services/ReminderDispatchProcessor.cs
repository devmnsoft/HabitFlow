using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class ReminderDispatchOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 50;
    public int LeaseSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
}

public static class ReminderDispatchEvents
{
    public static readonly EventId Claimed = new(61601, "reminder.dispatch.claimed");
    public static readonly EventId Delivered = new(61602, "reminder.dispatch.delivered");
    public static readonly EventId RetryScheduled = new(61603, "reminder.dispatch.retry_scheduled");
    public static readonly EventId Failed = new(61604, "reminder.dispatch.failed");
}

public sealed class ReminderDispatchProcessor(
    IReminderDispatchRepository repository, ReminderScheduleCalculator schedules,
    TimeProvider clock, ILogger<ReminderDispatchProcessor> logger)
{
    public async Task<ReminderDispatchResult> ProcessAsync(
        string workerId, ReminderDispatchOptions options, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var candidates = await repository.ClaimAsync(now, options.BatchSize, workerId,
            TimeSpan.FromSeconds(options.LeaseSeconds), ct);
        logger.LogInformation(ReminderDispatchEvents.Claimed,
            "Claimed {Count} reminder dispatches by worker {WorkerId}", candidates.Count, workerId);
        var delivered = 0;
        var retried = 0;
        var failed = 0;
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // The scheduled instant, rather than the processing time, is the recurrence anchor.
                var next = schedules.Next(candidate.ReminderTime, candidate.DaysOfWeek,
                    candidate.Timezone, candidate.ScheduledFor);
                await repository.CompleteAsync(candidate, next, clock.GetUtcNow(), ct);
                delivered++;
                logger.LogInformation(ReminderDispatchEvents.Delivered,
                    "Delivered reminder dispatch {DispatchId} correlation {CorrelationId}",
                    candidate.DispatchId, candidate.CorrelationId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                var attempt = candidate.AttemptCount + 1;
                var retryAt = attempt < options.MaxAttempts
                    ? clock.GetUtcNow().AddSeconds(Math.Min(300, 5 * Math.Pow(2, attempt - 1)))
                    : (DateTimeOffset?)null;
                var nextOccurrence = retryAt is null
                    ? schedules.Next(candidate.ReminderTime, candidate.DaysOfWeek, candidate.Timezone, candidate.ScheduledFor)
                    : (DateTimeOffset?)null;
                var willRetry = await repository.FailAsync(candidate, StableErrorCode(exception),
                    clock.GetUtcNow(), retryAt, nextOccurrence, ct);
                if (willRetry)
                {
                    retried++;
                    logger.LogWarning(ReminderDispatchEvents.RetryScheduled,
                        "Retry scheduled for dispatch {DispatchId} correlation {CorrelationId}",
                        candidate.DispatchId, candidate.CorrelationId);
                }
                else
                {
                    failed++;
                    logger.LogError(ReminderDispatchEvents.Failed,
                        "Reminder dispatch {DispatchId} permanently failed correlation {CorrelationId}",
                        candidate.DispatchId, candidate.CorrelationId);
                }
            }
        }
        return new(candidates.Count, delivered, retried, failed);
    }

    private static string StableErrorCode(Exception exception) => exception switch
    {
        ArgumentException => "invalid_schedule",
        TimeoutException => "infrastructure_timeout",
        _ => "dispatch_error"
    };
}

public sealed class ReminderDispatchRuntimeState(TimeProvider clock)
{
    private readonly object sync = new();
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTimeOffset? LastSuccessfulRunAt { get; private set; }
    public TimeSpan LastDuration { get; private set; }
    public int LastBatchSize { get; private set; }
    public string? LastErrorCode { get; private set; }

    public void Record(DateTimeOffset started, ReminderDispatchResult? result, bool succeeded, string? errorCode = null)
    {
        lock (sync)
        {
            LastRunAt = started;
            LastDuration = clock.GetUtcNow() - started;
            LastBatchSize = result?.Claimed ?? 0;
            LastErrorCode = errorCode;
            if (succeeded) LastSuccessfulRunAt = clock.GetUtcNow();
        }
    }
}

public sealed class ReminderDispatchHealthService(IReminderDispatchRepository repository, TimeProvider clock)
{
    public Task<ReminderDispatchHealth> SnapshotAsync(CancellationToken ct = default) =>
        repository.GetHealthAsync(clock.GetUtcNow(), ct);
}
