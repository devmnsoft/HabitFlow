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

    private static UserGoal Goal(GoalTargetType type, int current, int target) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Meta", null, type.ToString(), target,
        current, new DateOnly(2026, 1, 1), null, "Active", null, null, DateTime.UtcNow,
        DateTime.UtcNow, null);

    private static GoalProgressContext Context(UserGoal goal, GoalProgressSnapshot snapshot) => new(
        goal.ClientId, goal.UserId, goal.Id, Guid.NewGuid(), new DateOnly(2026, 1, 15),
        goal.StartDate, new DateOnly(2026, 1, 15), Guid.NewGuid(), "idem", "corr", snapshot);
}
