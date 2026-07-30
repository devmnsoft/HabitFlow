namespace HabitFlow.Domain;

public sealed record MilestoneEvaluationContext(
    Guid ClientId,
    Guid UserId,
    Guid TriggerHabitId,
    Guid SourceCompletionId,
    DateOnly LocalDate,
    int CurrentStreak,
    bool GoalCompletedNow,
    string CorrelationId);

public sealed record MilestoneEvaluationResult(Guid MilestoneId, string Code, string Title, string Message, DateTime AchievedAtUtc);

public interface IMilestoneRepository
{
    Task<IReadOnlyList<MilestoneEvaluationResult>> AwardEligibleAsync(MilestoneEvaluationContext context, CancellationToken ct = default);
}
