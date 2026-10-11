namespace HabitFlow.Domain;

public enum RoutineExperimentStatus { Draft, Active, Paused, Completed, Canceled }
public enum RoutineExperimentAdjustmentType { ChangeTime, ReduceFrequency, UseMinimumVersion, AdjustEstimatedDuration, ModifyReminder }
public enum RoutineExperimentIndicator { CompletionCount, CompletionRate, MinimumVersionCount, FullTargetCount }
public enum RoutineExperimentDecision { KeepCurrent, RestorePrevious, ReviewManually, None }
public enum RoutineExperimentLearningOutcome { Useful, Inconclusive, NotUseful }

public sealed record RoutineExperiment(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    Guid HabitId,
    string Name,
    string Difficulty,
    RoutineExperimentAdjustmentType AdjustmentType,
    string ProposedAdjustment,
    DateOnly TestStart,
    DateOnly TestEnd,
    DateOnly BaselineStart,
    DateOnly BaselineEnd,
    RoutineExperimentIndicator PrimaryIndicator,
    RoutineExperimentStatus Status,
    string? Reflection,
    string PreviousConfigurationJson,
    string ProposedConfigurationJson,
    RoutineExperimentDecision Decision,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ClosedAt);

public sealed record RoutineExperimentLearning(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    Guid HabitId,
    Guid ExperimentId,
    RoutineExperimentLearningOutcome Outcome,
    string Observation,
    bool IsPrivate,
    DateTimeOffset CreatedAt);

public sealed record RoutineExperimentEvent(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    Guid ExperimentId,
    string EventType,
    string Message,
    DateTimeOffset CreatedAt);
