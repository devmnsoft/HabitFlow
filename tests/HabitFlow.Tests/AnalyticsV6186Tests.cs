using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AnalyticsV6186Tests
{
    private readonly MetricCalculationService _metrics = new();

    [Fact]
    public void Daily_rate_uses_only_scheduled_occurrences()
    {
        var day = new DateOnly(2026, 8, 28);
        var result = _metrics.Calculate([new(Guid.NewGuid(), day, true), new(Guid.NewGuid(), day, false)], day);
        Assert.Equal(50, result.CompletionRate);
        Assert.Equal(1, result.MissedCount);
    }

    [Fact]
    public void Weekly_rate_ignores_duplicate_completion_and_future_period()
    {
        var habit = Guid.NewGuid(); var today = new DateOnly(2026, 8, 28);
        var rows = Enumerable.Range(0, 7).Select(i => new MetricOccurrence(habit, today.AddDays(-i), i < 4)).ToList();
        rows.Add(new(habit, today, true));
        rows.Add(new(habit, today.AddDays(1), true));
        Assert.Equal(57.1, _metrics.Calculate(rows, today).CompletionRate);
    }

    [Fact]
    public void Streaks_are_based_on_fully_completed_scheduled_days()
    {
        var habit = Guid.NewGuid(); var today = new DateOnly(2026, 8, 28);
        var result = _metrics.Calculate([new(habit,today.AddDays(-3),true),new(habit,today.AddDays(-2),false),new(habit,today.AddDays(-1),true),new(habit,today,true)],today);
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(2, result.BestStreak);
    }

    [Fact]
    public void Paused_archived_and_unscheduled_week_days_are_not_occurrences()
    {
        var date = new DateOnly(2026, 8, 28); // Friday
        var active = Habit(false, false, HabitFrequencyType.CustomWeekly);
        Assert.False(MetricCalculationService.IsScheduled(active, new HashSet<int>{1}, date));
        Assert.False(MetricCalculationService.IsScheduled(Habit(true,false), new HashSet<int>(), date));
        Assert.False(MetricCalculationService.IsScheduled(Habit(false,true), new HashSet<int>(), date));
    }

    [Fact]
    public void Empty_state_is_honest_zero()
    {
        var result = _metrics.Calculate([], new DateOnly(2026,8,28));
        Assert.Equal(0,result.CompletionRate); Assert.Equal(0,result.CurrentStreak); Assert.Equal(0,result.MissedCount);
    }

    private static Habit Habit(bool archived, bool paused, HabitFrequencyType frequency = HabitFrequencyType.Daily) =>
        new(Guid.NewGuid(),Guid.NewGuid(),"Leitura","#123456","Estudo",archived,null,new DateTime(2026,1,1),new DateTime(2026,1,1),frequency,ClientId:Guid.NewGuid(),IsPaused:paused);
}
