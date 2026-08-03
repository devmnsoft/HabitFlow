using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class EngagementV696Tests
{
    [Theory]
    [InlineData("/dashboard", true)]
    [InlineData("/habits/00000000-0000-0000-0000-000000000001", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("//example.com", false)]
    [InlineData("https://example.com", false)]
    public void Notification_actions_are_restricted_to_known_internal_routes(string url, bool expected) =>
        Assert.Equal(expected, new NotificationActionUrlValidator().IsSafe(url));

    [Fact]
    public void Reminder_schedule_uses_local_time_and_returns_utc()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero));
        var result = new ReminderScheduleCalculator(clock).Next(new TimeOnly(8, 0), [1,2,3,4,5], "America/Sao_Paulo");
        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 4, 11, 0, 0, TimeSpan.Zero), result);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
