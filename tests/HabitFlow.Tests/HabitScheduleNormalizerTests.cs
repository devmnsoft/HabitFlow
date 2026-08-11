using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitScheduleNormalizerTests
{
    private readonly HabitScheduleNormalizer subject = new();

    [Theory]
    [InlineData(HabitFrequencyType.Daily, 7)]
    [InlineData(HabitFrequencyType.Weekdays, 5)]
    [InlineData(HabitFrequencyType.Weekends, 2)]
    public void Fixed_frequencies_accept_null_days_and_apply_default(HabitFrequencyType frequency, int target)
    {
        var result = subject.Normalize(new(frequency, null, null));
        Assert.True(result.IsSuccess); Assert.Equal(target, result.Value!.TargetPerWeek); Assert.Empty(result.Value.SelectedDays);
    }

    [Fact]
    public void Daily_ignores_submitted_days()
    {
        var result = subject.Normalize(new(HabitFrequencyType.Daily, null, [1, 2, 7]));
        Assert.True(result.IsSuccess); Assert.Empty(result.Value!.SelectedDays);
    }

    [Theory]
    [InlineData(null)]
    public void Custom_requires_days(int[]? days)
    {
        var result = subject.Normalize(new(HabitFrequencyType.CustomWeekly, null, days));
        Assert.Equal("habit.custom_days_required", result.Error.Code);
    }

    [Fact]
    public void Custom_empty_requires_days() => Assert.Equal("habit.custom_days_required", subject.Normalize(new(HabitFrequencyType.CustomWeekly, null, [])).Error.Code);

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Invalid_weekday_is_rejected(int day) => Assert.Equal("habit.weekday_invalid", subject.Normalize(new(HabitFrequencyType.CustomWeekly, 1, [day])).Error.Code);

    [Fact]
    public void Custom_deduplicates_orders_and_defaults_target()
    {
        var result = subject.Normalize(new(HabitFrequencyType.CustomWeekly, null, [5, 1, 3, 1]));
        Assert.True(result.IsSuccess); Assert.Equal([1, 3, 5], result.Value!.SelectedDays); Assert.Equal(3, result.Value.TargetPerWeek);
    }

    [Theory]
    [InlineData(HabitFrequencyType.Daily, 0)]
    [InlineData(HabitFrequencyType.Daily, 8)]
    [InlineData(HabitFrequencyType.Weekdays, 6)]
    [InlineData(HabitFrequencyType.Weekends, 3)]
    public void Target_outside_frequency_capacity_is_rejected(HabitFrequencyType frequency, int target) =>
        Assert.Equal("habit.target_invalid", subject.Normalize(new(frequency, target, null)).Error.Code);

    [Fact]
    public void Unknown_frequency_is_rejected() => Assert.Equal("habit.frequency_invalid", subject.Normalize(new((HabitFrequencyType)999, null, null)).Error.Code);
}
