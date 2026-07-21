using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class UserUiPreferenceTests
{
    [Fact]
    public async Task Service_returns_default_when_user_has_no_preference()
    {
        var service = Service(new FakePreferenceRepo());
        var userId = Guid.NewGuid();
        var preference = await service.GetForUserAsync(userId);
        Assert.Equal(userId, preference.UserId);
        Assert.Equal(ContrastMode.Default, preference.ContrastMode);
        Assert.Equal(FontScale.Normal, preference.FontScale);
        Assert.False(preference.ReduceMotion);
    }

    [Fact]
    public async Task Service_saves_own_user_preference()
    {
        var repo = new FakePreferenceRepo();
        var service = Service(repo);
        var userId = Guid.NewGuid();
        await service.SaveAsync(userId, ContrastMode.HighContrast, FontScale.Large, true);
        var saved = await repo.GetByUserIdAsync(userId);
        Assert.NotNull(saved);
        Assert.Equal(ContrastMode.HighContrast, saved!.ContrastMode);
        Assert.Equal(FontScale.Large, saved.FontScale);
        Assert.True(saved.ReduceMotion);
    }

    [Fact]
    public async Task Service_does_not_change_another_user_preference_when_called_for_current_user()
    {
        var repo = new FakePreferenceRepo();
        var service = Service(repo);
        var currentUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await service.SaveAsync(otherUser, ContrastMode.Default, FontScale.Normal, false);
        await service.SaveAsync(currentUser, ContrastMode.HighContrast, FontScale.Large, true);
        Assert.Equal(ContrastMode.Default, (await repo.GetByUserIdAsync(otherUser))!.ContrastMode);
    }

    private static UserUiPreferenceService Service(IUserUiPreferenceRepository repo) => new(repo, new AuditService(new FakeAuditRepo(), new LogSanitizer(), NullLogger<AuditService>.Instance));

    private sealed class FakePreferenceRepo : IUserUiPreferenceRepository
    {
        private readonly Dictionary<Guid, UserUiPreference> store = [];
        public Task<UserUiPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(store.GetValueOrDefault(userId));
        public Task UpsertAsync(UserUiPreference preference, CancellationToken ct = default) { store[preference.UserId] = preference; return Task.CompletedTask; }
    }

    private sealed class FakeAuditRepo : IAuditRepository
    {
        public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SystemAuditLog>>([]);
    }
}
