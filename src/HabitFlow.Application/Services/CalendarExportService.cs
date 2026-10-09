using System.Globalization;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public interface ICalendarExportService
{
    Task<string> ExportAsync(CalendarFeed feed, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public sealed record CalendarEventDto(Guid HabitId, string Name, DateOnly Date, TimeOnly? Time, int? DurationMinutes);

public sealed class CalendarExportService(
    IHabitRepository habits,
    IHabitWeekDayRepository weekDays,
    IHabitScheduleExceptionRepository scheduleExceptions) : ICalendarExportService
{
    public async Task<string> ExportAsync(CalendarFeed feed, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidatePeriod(from, to);

        if (!feed.IncludeHabits)
            return FormatEvents(Array.Empty<CalendarEventDto>());

        var userHabits = await habits.ListActiveAsync(feed.ClientId, feed.UserId, ct);
        var selectedDays = await weekDays.ListByHabitsAsync(userHabits.Select(habit => habit.Id), ct);
        var exceptions = await scheduleExceptions.ListAsync(feed.ClientId, feed.UserId, from, to, ct);
        var events = BuildEvents(userHabits, selectedDays, exceptions, from, to);
        return FormatEvents(events);
    }

    public static IReadOnlyList<CalendarEventDto> BuildEvents(
        IReadOnlyList<Habit> habits,
        IReadOnlyDictionary<Guid, IReadOnlyList<HabitWeekDay>> selectedDays,
        IReadOnlyList<HabitScheduleException> exceptions,
        DateOnly from,
        DateOnly to)
    {
        ValidatePeriod(from, to);

        var activeHabits = habits.Where(habit => !habit.IsArchived && !habit.IsPaused).ToDictionary(habit => habit.Id);
        var changesByDate = exceptions
            .Where(exception => activeHabits.ContainsKey(exception.HabitId))
            .ToDictionary(exception => (exception.HabitId, exception.LocalDate));
        var occurrences = new Dictionary<(Guid HabitId, DateOnly Date), CalendarEventDto>();

        foreach (var habit in activeHabits.Values)
        {
            selectedDays.TryGetValue(habit.Id, out var days);
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (habit.StartDate is { } startDate && date < startDate)
                    continue;

                changesByDate.TryGetValue((habit.Id, date), out var change);
                if (change?.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved)
                    continue;

                if (change?.Type == HabitScheduleExceptionType.Added || IsDueOn(habit, date, days ?? Array.Empty<HabitWeekDay>()))
                    AddOccurrence(occurrences, habit, date);
            }
        }

        foreach (var change in exceptions.Where(exception => exception.Type == HabitScheduleExceptionType.Moved
                     && exception.DestinationDate is { } destination && destination >= from && destination <= to))
        {
            if (activeHabits.TryGetValue(change.HabitId, out var habit))
                AddOccurrence(occurrences, habit, change.DestinationDate!.Value);
        }

        return occurrences.Values
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Time)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string FormatEvents(IEnumerable<CalendarEventDto> events, DateTimeOffset? generatedAt = null)
    {
        var output = new StringBuilder();
        AppendLine(output, "BEGIN:VCALENDAR");
        AppendLine(output, "VERSION:2.0");
        AppendLine(output, "PRODID:-//HabitFlow//Calendar 6.19.9//PT-BR");
        AppendLine(output, "CALSCALE:GREGORIAN");
        AppendLine(output, "METHOD:PUBLISH");
        AppendLine(output, "X-WR-CALNAME:HabitFlow");

        var timestamp = (generatedAt ?? DateTimeOffset.UtcNow).ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        foreach (var item in events)
        {
            AppendLine(output, "BEGIN:VEVENT");
            AppendLine(output, $"UID:{item.HabitId:N}-{item.Date:yyyyMMdd}@habitflow");
            AppendLine(output, $"DTSTAMP:{timestamp}");
            if (item.Time is { } time)
            {
                var start = item.Date.ToDateTime(time).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
                var end = item.Date.ToDateTime(time).AddMinutes(Math.Clamp(item.DurationMinutes ?? 15, 1, 1440))
                    .ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
                AppendLine(output, $"DTSTART:{start}");
                AppendLine(output, $"DTEND:{end}");
            }
            else
            {
                AppendLine(output, $"DTSTART;VALUE=DATE:{item.Date:yyyyMMdd}");
                AppendLine(output, $"DTEND;VALUE=DATE:{item.Date.AddDays(1):yyyyMMdd}");
            }

            AppendLine(output, $"SUMMARY:{Escape(item.Name)}");
            AppendLine(output, "END:VEVENT");
        }

        AppendLine(output, "END:VCALENDAR");
        return output.ToString();
    }

    private static bool IsDueOn(Habit habit, DateOnly date, IReadOnlyCollection<HabitWeekDay> days)
    {
        var day = (int)date.DayOfWeek;
        return habit.FrequencyType switch
        {
            HabitFrequencyType.Daily => true,
            HabitFrequencyType.Weekdays => day is >= 1 and <= 5,
            HabitFrequencyType.Weekends => day is 0 or 6,
            HabitFrequencyType.CustomWeekly => days.Any(item => item.DayOfWeek == day),
            _ => false
        };
    }

    public static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (!IsValidPeriod(from, to))
            throw new ArgumentException("O período deve ter até 366 dias e permitir a data final exclusiva do calendário.");
    }

    public static bool IsValidPeriod(DateOnly from, DateOnly to) =>
        from <= to && to != DateOnly.MaxValue && to.DayNumber - from.DayNumber < 366;

    private static void AddOccurrence(
        IDictionary<(Guid HabitId, DateOnly Date), CalendarEventDto> occurrences,
        Habit habit,
        DateOnly date)
    {
        occurrences.TryAdd((habit.Id, date), new(habit.Id, habit.Name, date, habit.ReminderTime, habit.EstimatedTimeMinutes));
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private static void AppendLine(StringBuilder output, string value)
    {
        var bytes = new byte[4];
        var bytesOnLine = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeLength = rune.EncodeToUtf8(bytes);
            if (bytesOnLine + runeLength > 75)
            {
                output.Append("\r\n ");
                bytesOnLine = 1;
            }

            output.Append(rune.ToString());
            bytesOnLine += runeLength;
        }

        output.Append("\r\n");
    }
}
