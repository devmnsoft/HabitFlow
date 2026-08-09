using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class GlobalSearchV6106Tests
{
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Search_rejects_missing_tenant_context_without_querying_repositories()
    {
        var habits = new HabitRepositoryStub([]);
        var goals = new GoalRepositoryStub([]);
        var service = new GlobalSearchService(habits, goals, new HabitLibraryFallbackProvider());

        var result = await service.SearchAsync(Guid.Empty, _userId, "privacidade");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Search_only_uses_tenant_scoped_habit_and_goal_queries()
    {
        var habits = new HabitRepositoryStub([new Habit(Guid.NewGuid(), _userId, "Meditar", "#10b981", "Bem-estar", false, null, DateTime.UtcNow, DateTime.UtcNow, Notes: "Respirar por dez minutos", ClientId: _clientId)]);
        var goals = new GoalRepositoryStub([]);
        var service = new GlobalSearchService(habits, goals, new HabitLibraryFallbackProvider());

        var result = await service.SearchAsync(_clientId, _userId, "Meditar");

        var habit = Assert.Single(result.Where(item => item.Type == "Hábito"));
        Assert.Equal("Meditar", habit.Title);
        Assert.StartsWith("/habits/", habit.Url);
        Assert.Equal((_clientId, _userId), habits.LastScope);
        Assert.Equal((_clientId, _userId), goals.LastScope);
    }

    [Fact]
    public async Task Search_includes_safe_product_destinations_and_honors_limit()
    {
        var habits = new HabitRepositoryStub([]);
        var goals = new GoalRepositoryStub([]);
        var service = new GlobalSearchService(habits, goals, new HabitLibraryFallbackProvider());

        var result = await service.SearchAsync(_clientId, _userId, "plano", 1);

        var destination = Assert.Single(result);
        Assert.Equal("/account/plan/usage", destination.Url);
    }

    private sealed class HabitRepositoryStub(IReadOnlyList<Habit> values) : IHabitRepository
    {
        public (Guid, Guid)? LastScope { get; private set; }
        public Task<IReadOnlyList<Habit>> ListActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) { LastScope = (clientId, userId); return Task.FromResult(values); }
        public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Habit>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Habit?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, Guid idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Habit?> FindActiveBySourceTemplateAsync(Guid clientId, Guid userId, Guid templateId, bool includeVariations, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Habit?> GetAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task CreateAsync(Habit habit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Guid clientId, Guid userId, Habit habit, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class GoalRepositoryStub(IReadOnlyList<UserGoal> values) : IUserGoalRepository
    {
        public (Guid, Guid)? LastScope { get; private set; }
        public Task<IReadOnlyList<UserGoal>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) { LastScope = (clientId, userId); return Task.FromResult(values); }
        public Task<UserGoal?> GetAsync(Guid id, Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task CreateAsync(UserGoal goal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(UserGoal goal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetStatusAsync(Guid id, Guid clientId, Guid userId, string status, CancellationToken ct = default) => throw new NotSupportedException();
        public Task LinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UnlinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Habit>> ListLinkedHabitsAsync(Guid goalId, Guid clientId, Guid userId, CancellationToken ct = default)
    => Task.FromResult((IReadOnlyList<Habit>)Array.Empty<Habit>());

public Task<IReadOnlyList<GoalTimelineEntry>> ListTimelineAsync(Guid goalId, Guid clientId, Guid userId, CancellationToken ct = default)
    => Task.FromResult((IReadOnlyList<GoalTimelineEntry>)Array.Empty<GoalTimelineEntry>());
    }
}
