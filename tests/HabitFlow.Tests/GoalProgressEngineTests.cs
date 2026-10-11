using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class GoalProgressEngineTests
{
    public static IEnumerable<object[]> AutomaticTargets()
    {
        yield return [GoalTargetType.HabitCompletions, 8];
        yield return [GoalTargetType.ActiveDays, 4];
        yield return [GoalTargetType.StreakDays, 3];
        yield return [GoalTargetType.WeeklyCompletions, 5];
    }

    [Theory]
    [MemberData(nameof(AutomaticTargets))]
    public void Calculator_uses_canonical_value_for_each_automatic_target(GoalTargetType type, int expected)
    {
        var goal = Goal(type, current: 1, target: 20);
        var context = Context(goal, new(8, 4, 3, 5));

        var result = new GoalProgressCalculator().Calculate(goal, context, false);

        Assert.Equal(expected, result.CurrentValue);
        Assert.Equal(1, result.PreviousValue);
        Assert.False(result.CompletedNow);
    }

    [Fact]
    public void Custom_target_is_never_changed_automatically()
    {
        var goal = Goal(GoalTargetType.Custom, current: 7, target: 10);
        var result = new GoalProgressCalculator().Calculate(goal, Context(goal, new(99, 99, 99, 99)), false);
        Assert.Equal(7, result.CurrentValue);
    }

    [Fact]
    public void Completion_is_detected_once_and_completed_timestamp_is_preserved_on_undo()
    {
        var calculator = new GoalProgressCalculator();
        var active = Goal(GoalTargetType.ActiveDays, current: 2, target: 3);
        var completed = calculator.Calculate(active, Context(active, new(3, 3, 1, 3)), false);
        Assert.True(completed.CompletedNow);
        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(completed.CompletedAtUtc);

        var historical = active with { CurrentValue = 3, Status = "Completed", CompletedAt = completed.CompletedAtUtc };
        var corrected = calculator.Calculate(historical, Context(historical, new(2, 2, 1, 2)), true);
        Assert.False(corrected.CompletedNow);
        Assert.Equal(completed.CompletedAtUtc, corrected.CompletedAtUtc);
        Assert.Equal("Completed", corrected.Status);
    }

    [Fact]
    public void Completion_uses_injected_clock_for_deterministic_timestamp()
    {
        var now = new DateTimeOffset(2026, 1, 20, 14, 30, 0, TimeSpan.Zero);
        var calculator = new GoalProgressCalculator(new FixedTimeProvider(now));
        var goal = Goal(GoalTargetType.HabitCompletions, current: 2, target: 3);

        var result = calculator.Calculate(goal, Context(goal, new(3, 2, 1, 3)), false);

        Assert.True(result.CompletedNow);
        Assert.Equal(now.UtcDateTime, result.CompletedAtUtc);
    }

    [Fact]
    public async Task Engine_does_not_recalculate_paused_or_canceled_goals()
    {
        var active = Goal(GoalTargetType.HabitCompletions, current: 0, target: 1);
        var repository = new CapturingGoalProgressRepository([
            active,
            active with { Id = Guid.NewGuid(), Status = "Paused" },
            active with { Id = Guid.NewGuid(), Status = "Canceled" }
        ], new(1, 1, 1, 1));
        var engine = new GoalProgressEngine(repository, new GoalProgressCalculator(new FixedTimeProvider(new(2026, 1, 20, 12, 0, 0, TimeSpan.Zero))), new FixedTimeProvider(new(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)));

        var results = await engine.RecalculateAsync(active.ClientId, active.UserId, Guid.NewGuid(), new(2026, 1, 20), Guid.NewGuid(), "idem", "corr", 1, false);

        Assert.Single(results);
        Assert.Equal(1, repository.ApplyCount);
    }

    private static UserGoal Goal(GoalTargetType type, int current, int target) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Meta", null, type.ToString(), target,
        current, new DateOnly(2026, 1, 1), null, "Active", null, null, DateTime.UtcNow,
        DateTime.UtcNow, null);

    private static GoalProgressContext Context(UserGoal goal, GoalProgressSnapshot snapshot) => new(
        goal.ClientId, goal.UserId, goal.Id, Guid.NewGuid(), new DateOnly(2026, 1, 15),
        goal.StartDate, new DateOnly(2026, 1, 15), Guid.NewGuid(), "idem", "corr", snapshot);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CapturingGoalProgressRepository(IReadOnlyList<UserGoal> goals, GoalProgressSnapshot snapshot) : IGoalProgressRepository
    {
        public int ApplyCount { get; private set; }

        public Task<IReadOnlyList<UserGoal>> ListRelatedAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) =>
            Task.FromResult(goals);

        public Task<GoalProgressSnapshot> BuildSnapshotAsync(UserGoal goal, Guid triggerHabitId, DateOnly localDate, int currentStreak, CancellationToken ct = default) =>
            Task.FromResult(snapshot);

        public Task<bool> ApplyAsync(UserGoal goal, GoalProgressResult progressResult, GoalProgressEvent progressEvent, CancellationToken ct = default)
        {
            ApplyCount++;
            return Task.FromResult(true);
        }
    }
}
