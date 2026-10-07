using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitCalendarExportTests
{
    [Fact]
    public void BuildEvents_UsesFrequencyAndSelectedCustomWeekdaysWithinRequestedPeriod()
    {
        var weekdays = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Weekdays", "#000", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow, HabitFrequencyType.Weekdays);
        var custom = new Habit(Guid.NewGuid(), weekdays.UserId, "Custom", "#000", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow, HabitFrequencyType.CustomWeekly);
        var monday = new DateOnly(2026, 10, 5);
        var selectedDays = new Dictionary<Guid, IReadOnlyList<HabitWeekDay>>
        {
            [custom.Id] = [new(Guid.NewGuid(), custom.Id, (int)monday.DayOfWeek, DateTime.UtcNow)]
        };

        var events = CalendarExportService.BuildEvents([weekdays, custom], selectedDays, [], monday, monday.AddDays(2));

        Assert.Equal(4, events.Count);
        Assert.Equal(3, events.Count(item => item.HabitId == weekdays.Id));
        Assert.Single(events, item => item.HabitId == custom.Id);
        Assert.Contains(events, item => item.HabitId == custom.Id && item.Date == monday);
        Assert.DoesNotContain(events, item => item.HabitId == weekdays.Id && item.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
    }

    [Fact]
    public void BuildEvents_AppliesExcusedMovedAndAddedScheduleChanges()
    {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var habit = new Habit(Guid.NewGuid(), userId, "Daily", "#000", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow);
        var from = new DateOnly(2026, 10, 5);
        var exceptions = new[]
        {
            CreateException(habit, clientId, from, HabitScheduleExceptionType.Excused),
            CreateException(habit, clientId, from.AddDays(1), HabitScheduleExceptionType.Moved, from.AddDays(4)),
            CreateException(habit, clientId, from.AddDays(2), HabitScheduleExceptionType.Added)
        };

        var events = CalendarExportService.BuildEvents([habit], new Dictionary<Guid, IReadOnlyList<HabitWeekDay>>(),
            exceptions, from, from.AddDays(4));

        Assert.Equal([from.AddDays(2), from.AddDays(3), from.AddDays(4)],
            events.Select(item => item.Date).Order().ToArray());
    }

    [Fact]
    public void FormatEvents_EscapesTextAndUsesRequestedDateAndTime()
    {
        var item = new CalendarEventDto(Guid.NewGuid(), "Leitura, capítulo; nota\nprivada", new(2026, 10, 7),
            new(8, 30), 30);

        var calendar = CalendarExportService.FormatEvents([item], new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("DTSTART:20261007T083000", calendar);
        Assert.Contains("DTEND:20261007T090000", calendar);
        Assert.Contains("SUMMARY:Leitura\\, capítulo\\; nota\\nprivada", calendar);
        Assert.DoesNotContain("RRULE:FREQ=DAILY", calendar);
        Assert.All(calendar.Split("\r\n", StringSplitOptions.RemoveEmptyEntries),
            line => Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 75, $"ICS line exceeds 75 octets: {line}"));
    }

    [Fact]
    public void ValidatePeriod_RejectsReversedOversizedAndUnrepresentableExclusiveEnd()
    {
        var date = new DateOnly(2026, 10, 7);

        Assert.Throws<ArgumentException>(() => CalendarExportService.ValidatePeriod(date.AddDays(1), date));
        Assert.Throws<ArgumentException>(() => CalendarExportService.ValidatePeriod(date, date.AddDays(366)));
        Assert.Throws<ArgumentException>(() => CalendarExportService.ValidatePeriod(date, DateOnly.MaxValue));
    }

    private static HabitScheduleException CreateException(
        Habit habit,
        Guid clientId,
        DateOnly date,
        HabitScheduleExceptionType type,
        DateOnly? destination = null) =>
        new(Guid.NewGuid(), clientId, habit.UserId, habit.Id, date, type, destination, null, 1,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
