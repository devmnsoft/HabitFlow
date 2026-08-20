using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ReminderOperationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Snooze_moves_only_an_owned_active_reminder()
    {
        var reminder = ActiveReminder();
        var repository = new ReminderRepositoryFake(reminder);
        var clock = new FixedTimeProvider(Now);
        var service = new HabitReminderService(repository, new ReminderScheduleCalculator(clock), clock);

        var result = await service.SnoozeAsync(reminder.ClientId, reminder.UserId, reminder.Id, 15);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddMinutes(15), repository.SnoozedUntil);
    }

    [Fact]
    public async Task Delete_is_scoped_by_client_and_user()
    {
        var reminder = ActiveReminder();
        var repository = new ReminderRepositoryFake(reminder);
        var clock = new FixedTimeProvider(Now);
        var service = new HabitReminderService(repository, new ReminderScheduleCalculator(clock), clock);

        var result = await service.DeleteAsync(Guid.NewGuid(), reminder.UserId, reminder.Id);

        Assert.True(result.IsFailure);
        Assert.NotNull(repository.Reminder);
    }

    private static HabitReminder ActiveReminder() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "Caminhar", new TimeOnly(8, 0), "UTC", [1, 2, 3, 4, 5], true, null, Now, Now.UtcDateTime, Now.UtcDateTime);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ReminderRepositoryFake(HabitReminder reminder) : IHabitReminderRepository
    {
        public HabitReminder? Reminder { get; private set; } = reminder;
        public DateTimeOffset? SnoozedUntil { get; private set; }
        public Task<IReadOnlyList<HabitReminder>> ListAsync(Guid clientId, Guid userId, Guid? habitId = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabitReminder>>([]);
        public Task<int> CountForHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<HabitReminder?> GetOwnedAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(Reminder is { } value && value.ClientId == clientId && value.UserId == userId && value.Id == id ? Reminder : null);
        public Task<bool> HabitBelongsToUserAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => Task.FromResult(false);
        public Task CreateAsync(HabitReminder value, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> SetActiveAsync(Guid clientId, Guid userId, Guid id, bool active, DateTimeOffset? next, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> SnoozeAsync(Guid clientId, Guid userId, Guid id, DateTimeOffset next, CancellationToken ct = default)
        {
            var owned = Reminder is { } value && value.ClientId == clientId && value.UserId == userId && value.Id == id && value.IsActive;
            if (owned) SnoozedUntil = next;
            return Task.FromResult(owned);
        }
        public Task<bool> DeleteAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
        {
            var owned = Reminder is { } value && value.ClientId == clientId && value.UserId == userId && value.Id == id;
            if (owned) Reminder = null;
            return Task.FromResult(owned);
        }
    }
}
