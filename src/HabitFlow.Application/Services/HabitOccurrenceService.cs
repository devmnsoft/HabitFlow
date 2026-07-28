using HabitFlow.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitFlow.Application;

public sealed record HabitOccurrence(ProgressHabitRow Habit, DateOnly Date);

public sealed class HabitOccurrenceService(ILogger<HabitOccurrenceService>? logger = null)
{
    public bool IsScheduledForDate(ProgressHabitRow habit, IReadOnlySet<int> weekDays, DateOnly date, TimeZoneInfo timeZone)
    {
        var created = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(habit.CreatedAt, DateTimeKind.Utc), timeZone));
        if (date < created) return false;
        if (habit.ArchivedAt.HasValue)
        {
            var archived = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(habit.ArchivedAt.Value, DateTimeKind.Utc), timeZone));
            if (date > archived) return false;
        }
        var day = (int)date.DayOfWeek;
        if (!Enum.TryParse<HabitFrequencyType>(habit.FrequencyTypeCode, true, out var frequency) ||
            frequency is not (HabitFrequencyType.Daily or HabitFrequencyType.Weekdays or HabitFrequencyType.Weekends or HabitFrequencyType.CustomWeekly))
        {
            (logger ?? NullLogger<HabitOccurrenceService>.Instance).LogWarning(
                "Unknown habit frequency code {FrequencyTypeCode} for habit {HabitId}; occurrence ignored.",
                habit.FrequencyTypeCode, habit.Id);
            return false;
        }
        return frequency switch
        {
            HabitFrequencyType.Daily => true,
            HabitFrequencyType.Weekdays => day is >= 1 and <= 5,
            HabitFrequencyType.Weekends => day is 0 or 6,
            HabitFrequencyType.CustomWeekly => weekDays.Contains(day),
            _ => false
        };
    }

    public Task<bool> IsScheduledForDateAsync(ProgressHabitRow habit, IReadOnlySet<int> days, DateOnly date, TimeZoneInfo zone) => Task.FromResult(IsScheduledForDate(habit, days, date, zone));
    public Task<IReadOnlyList<HabitOccurrence>> ListScheduledForDateAsync(IEnumerable<ProgressHabitRow> habits, IReadOnlyDictionary<Guid, IReadOnlySet<int>> days, DateOnly date, TimeZoneInfo zone) =>
        Task.FromResult<IReadOnlyList<HabitOccurrence>>(habits.Where(h => IsScheduledForDate(h, days.GetValueOrDefault(h.Id) ?? new HashSet<int>(), date, zone)).Select(h => new HabitOccurrence(h, date)).ToList());
    public async Task<IReadOnlyList<HabitOccurrence>> ListScheduledForPeriodAsync(IEnumerable<ProgressHabitRow> habits, IReadOnlyDictionary<Guid, IReadOnlySet<int>> days, DateOnly start, DateOnly end, TimeZoneInfo zone)
    {
        var result = new List<HabitOccurrence>();
        for (var date = start; date <= end; date = date.AddDays(1)) result.AddRange(await ListScheduledForDateAsync(habits, days, date, zone));
        return result;
    }
    public async Task<int> CountScheduledOccurrencesAsync(IEnumerable<ProgressHabitRow> habits, IReadOnlyDictionary<Guid, IReadOnlySet<int>> days, DateOnly start, DateOnly end, TimeZoneInfo zone) => (await ListScheduledForPeriodAsync(habits, days, start, end, zone)).Count;
    public async Task<DateOnly?> GetNextOccurrenceAsync(IEnumerable<ProgressHabitRow> habits, IReadOnlyDictionary<Guid, IReadOnlySet<int>> days, DateOnly after, TimeZoneInfo zone)
    {
        for (var date = after.AddDays(1); date <= after.AddYears(1); date = date.AddDays(1)) if ((await ListScheduledForDateAsync(habits, days, date, zone)).Count > 0) return date;
        return null;
    }
}
