using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Tests;

public sealed class ProgressCalendarV662Tests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static ProgressHabitRow Habit(HabitFrequencyType frequency, DateTime? created = null, DateTime? archived = null) => new()
    {
        Id = Guid.NewGuid(), Name = "Ler", FrequencyTypeCode = frequency.ToString(),
        CreatedAt = created ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), ArchivedAt = archived
    };

    [Theory]
    [InlineData(HabitFrequencyType.Daily, "2026-02-07", true)]
    [InlineData(HabitFrequencyType.Weekdays, "2026-02-09", true)]
    [InlineData(HabitFrequencyType.Weekdays, "2026-02-08", false)]
    [InlineData(HabitFrequencyType.Weekends, "2026-02-08", true)]
    [InlineData(HabitFrequencyType.Weekends, "2026-02-09", false)]
    public void Occurrence_respects_standard_frequencies(HabitFrequencyType frequency, string date, bool expected)
    {
        Assert.Equal(expected, new HabitOccurrenceService().IsScheduledForDate(Habit(frequency), new HashSet<int>(), DateOnly.Parse(date), Zone));
    }

    [Fact]
    public void Occurrence_respects_custom_creation_and_archive_boundaries()
    {
        var service = new HabitOccurrenceService(); var habit = Habit(HabitFrequencyType.CustomWeekly,
            new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 17, 12, 0, 0, DateTimeKind.Utc));
        var tuesday = new HashSet<int> { 2 };
        Assert.False(service.IsScheduledForDate(habit, tuesday, new DateOnly(2026, 2, 3), Zone));
        Assert.True(service.IsScheduledForDate(habit, tuesday, new DateOnly(2026, 2, 10), Zone));
        Assert.False(service.IsScheduledForDate(habit, tuesday, new DateOnly(2026, 2, 24), Zone));
    }

    [Theory]
    [InlineData(2025, 2, 28)] [InlineData(2024, 2, 29)] [InlineData(2026, 4, 30)] [InlineData(2026, 7, 31)]
    public async Task Occurrence_counts_every_day_in_month(int year, int month, int expected)
    {
        var habit = Habit(HabitFrequencyType.Daily); var service = new HabitOccurrenceService(); var start = new DateOnly(year, month, 1);
        Assert.Equal(expected, await service.CountScheduledOccurrencesAsync([habit], new Dictionary<Guid, IReadOnlySet<int>>(), start, start.AddMonths(1).AddDays(-1), Zone));
    }

    [Fact]
    public void Consistency_ignores_unscheduled_days_and_breaks_on_partial_day()
    {
        var result = new ConsistencyService().Calculate([(new(2026, 7, 20), 2, 2), (new(2026, 7, 21), 0, 0), (new(2026, 7, 22), 2, 1), (new(2026, 7, 23), 1, 1)], new(2026, 7, 23));
        Assert.Equal(1, result.CurrentStreak); Assert.Equal(1, result.BestStreak); Assert.Equal(3, result.ActiveDays);
    }

    [Fact]
    public void Summary_uses_scheduled_occurrences_and_never_active_habits_times_days()
    {
        var service = new ProgressCalendarService(null!, null!, new ConsistencyService(), null!, null!);
        var summary = service.BuildSummary([(new(2026, 7, 1), 2, 1), (new(2026, 7, 2), 0, 0), (new(2026, 7, 3), 1, 1)], new(2026, 7, 3));
        Assert.Equal(3, summary.ScheduledCount); Assert.Equal(2, summary.CompletedCount); Assert.Equal(66.7m, summary.CompletionPercentage);
    }

    [Fact]
    public void Consistency_crosses_month_and_ignores_days_without_schedule()
    {
        var result = new ConsistencyService().Calculate([
            (new(2026, 1, 31), 1, 1), (new(2026, 2, 1), 0, 0), (new(2026, 2, 2), 1, 1)], new(2026, 2, 2));
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(2, result.BestStreak);
    }

    [Fact]
    public void Incomplete_current_day_does_not_break_streak()
    {
        var result = new ConsistencyService().Calculate([
            (new(2026, 7, 27), 1, 1), (new(2026, 7, 28), 2, 1)], new(2026, 7, 28));
        Assert.Equal(1, result.CurrentStreak);
    }

    [Fact]
    public void Unknown_frequency_is_ignored_without_throwing()
    {
        var habit = Habit(HabitFrequencyType.Daily);
        habit.FrequencyTypeCode = "unexpected";
        Assert.False(new HabitOccurrenceService().IsScheduledForDate(habit, new HashSet<int>(), new(2026, 7, 28), Zone));
    }
}
