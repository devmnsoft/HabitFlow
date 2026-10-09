using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6250OfflineSyncTests
{
    [Fact]
    public async Task CompleteHabit_IsIdempotentAndMarksSynced()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var habit = new Habit(habitId, userId, "Hábito Teste", "#10B981", "Saúde", false, null, DateTime.UtcNow, DateTime.UtcNow, HabitFrequencyType.Daily, null, null, null, 0, clientId);

        var repo = new OfflineSyncRepoMock();
        var habits = new HabitRepoMock([habit]);
        var completions = new CompletionRepoMock();
        var audit = V6250TestStubs.CreateAudit();

        var service = new OfflineSyncService(repo, habits, completions, audit, NullLogger<OfflineSyncService>.Instance);

        var request = new OfflineActionRequest(
            Id: Guid.NewGuid(),
            ActionType: OfflineActionTypes.CompleteHabit,
            EntityId: habitId,
            PayloadJson: null,
            ClientCreatedAt: DateTime.UtcNow
        );

        var result = await service.ProcessSyncBatchAsync(clientId, userId, [request]);

        Assert.Single(result.Results);
        Assert.Equal(OfflineSyncStatus.Synced, result.Results[0].Status);
        Assert.Single(completions.Completions);

        // Re-executing same action simulates idempotent re-sync
        var reSync = await service.ProcessSyncBatchAsync(clientId, userId, [request]);
        Assert.Single(reSync.Results);
        Assert.Equal(OfflineSyncStatus.Synced, reSync.Results[0].Status);
    }

    [Fact]
    public async Task CompleteHabit_NonExistentHabit_ReturnsConflict()
    {
        var repo = new OfflineSyncRepoMock();
        var habits = new HabitRepoMock();
        var completions = new CompletionRepoMock();
        var audit = V6250TestStubs.CreateAudit();

        var service = new OfflineSyncService(repo, habits, completions, audit, NullLogger<OfflineSyncService>.Instance);

        var request = new OfflineActionRequest(
            Id: Guid.NewGuid(),
            ActionType: OfflineActionTypes.CompleteHabit,
            EntityId: Guid.NewGuid(),
            PayloadJson: null,
            ClientCreatedAt: DateTime.UtcNow
        );

        var result = await service.ProcessSyncBatchAsync(Guid.NewGuid(), Guid.NewGuid(), [request]);

        Assert.Single(result.Results);
        Assert.Equal(OfflineSyncStatus.Conflict, result.Results[0].Status);
        Assert.Contains("não foi encontrado", result.Results[0].ErrorMessage!);
    }

    [Fact]
    public async Task CreateDraftHabit_CreatesHabitWithProvidedPayload()
    {
        var repo = new OfflineSyncRepoMock();
        var habits = new HabitRepoMock();
        var completions = new CompletionRepoMock();
        var audit = V6250TestStubs.CreateAudit();

        var service = new OfflineSyncService(repo, habits, completions, audit, NullLogger<OfflineSyncService>.Instance);

        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var payload = "{\"Name\":\"Ler 10 páginas\",\"Category\":\"Desenvolvimento\",\"Color\":\"#10B981\"}";

        var request = new OfflineActionRequest(
            Id: Guid.NewGuid(),
            ActionType: OfflineActionTypes.CreateDraftHabit,
            EntityId: null,
            PayloadJson: payload,
            ClientCreatedAt: DateTime.UtcNow
        );

        var result = await service.ProcessSyncBatchAsync(clientId, userId, [request]);

        Assert.Single(result.Results);
        Assert.Equal(OfflineSyncStatus.Synced, result.Results[0].Status);
        Assert.Single(habits.Habits);
        Assert.Equal("Ler 10 páginas", habits.Habits[0].Name);
        Assert.Equal(clientId, habits.Habits[0].ClientId);
        Assert.Equal(userId, habits.Habits[0].UserId);
    }
}
