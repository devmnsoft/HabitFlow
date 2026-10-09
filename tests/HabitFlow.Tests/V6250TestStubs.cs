using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitFlow.Tests;

internal static class V6250TestStubs
{
    public static AuditService CreateAudit() =>
        new(new FakeAuditRepo(), new LogSanitizer(), NullLogger<AuditService>.Instance);
}

internal sealed class FakeAuditRepo : IAuditRepository
{
    public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SystemAuditLog>>([]);
}

internal sealed class FakePlanCatalogRepo : IPlanCatalogRepository
{
    public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PublicPlan>>([]);
    public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
        Task.FromResult<ClientPlanAccess?>(new ClientPlanAccess(clientId, "pro", "pro", "active", null, null));
    public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<Guid?>(Guid.NewGuid());
    public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, PlanFeatureValue>>(new Dictionary<string, PlanFeatureValue>
        {
            [PlanFeatureCodes.Webhooks] = new(PlanFeatureCodes.Webhooks, "Webhooks", "boolean", true, null, null),
            [PlanFeatureCodes.PublicApi] = new(PlanFeatureCodes.PublicApi, "Public API", "boolean", true, null, null),
            [PlanFeatureCodes.DataPortability] = new(PlanFeatureCodes.DataPortability, "Data Portability", "boolean", true, null, null),
            [PlanFeatureCodes.PushNotifications] = new(PlanFeatureCodes.PushNotifications, "Push Notifications", "boolean", true, null, null),
            [PlanFeatureCodes.AdvancedOffline] = new(PlanFeatureCodes.AdvancedOffline, "Advanced Offline", "boolean", true, null, null),
            [PlanFeatureCodes.AiAssistant] = new(PlanFeatureCodes.AiAssistant, "AI Assistant", "boolean", true, null, null)
        });
    public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) => Task.FromResult(true);
    public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
}

internal sealed class HabitRepoMock(List<Habit>? initial = null) : IHabitRepository
{
    public List<Habit> Habits { get; } = initial ?? [];

    public Task<IReadOnlyList<Habit>> ListActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Habit>>(Habits.Where(h => !h.IsArchived).ToList());

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Habits.Count(h => h.UserId == userId && !h.IsArchived));

    public Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Habit>>(Habits.Where(h => h.UserId == userId).ToList());

    public Task<IReadOnlyList<Habit>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Habit>>(Habits.Where(h => h.ClientId == clientId && h.UserId == userId).ToList());

    public Task<Habit?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, Guid idempotencyKey, CancellationToken ct = default) =>
        Task.FromResult<Habit?>(null);

    public Task<Habit?> FindActiveBySourceTemplateAsync(Guid clientId, Guid userId, Guid templateId, bool includeVariations, CancellationToken ct = default) =>
        Task.FromResult<Habit?>(null);

    public Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Habits.Count(h => h.ClientId == clientId && h.UserId == userId && !h.IsArchived));

    public Task<Habit?> GetAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) =>
        Task.FromResult(Habits.FirstOrDefault(h => h.Id == habitId));

    public Task CreateAsync(Habit habit, CancellationToken ct = default)
    {
        Habits.Add(habit);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Guid clientId, Guid userId, Habit habit, CancellationToken ct = default)
    {
        var idx = Habits.FindIndex(h => h.Id == habit.Id);
        if (idx >= 0) Habits[idx] = habit;
        return Task.FromResult(idx >= 0);
    }
}

internal sealed class CompletionRepoMock : IHabitCompletionRepository
{
    public List<(Guid HabitId, DateOnly Date)> Completions { get; } = [];

    public Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<HabitCompletion>>([]);

    public Task<IReadOnlyList<HabitCompletion>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<HabitCompletion>>([]);

    public Task<CompletionMutationResult> AddIfMissingAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, Guid completionId, CancellationToken ct = default)
    {
        var existing = Completions.Any(c => c.HabitId == habitId && c.Date == localDate);
        if (!existing) Completions.Add((habitId, localDate));
        return Task.FromResult(new CompletionMutationResult(completionId, !existing, false, true, localDate));
    }

    public Task<CompletionMutationResult> DeleteIfExistsAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, CancellationToken ct = default)
    {
        var count = Completions.RemoveAll(c => c.HabitId == habitId && c.Date == localDate);
        return Task.FromResult(new CompletionMutationResult(null, false, count > 0, false, localDate));
    }

    public Task AddAsync(HabitCompletion completion, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid habitId, Guid userId, DateOnly date, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class NotificationRepoMock : INotificationRepository
{
    public List<Notification> Notifications { get; } = [];

    public Task CreateAsync(Notification notification, CancellationToken ct = default)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Notification>>(Notifications.Where(n => !n.IsRead).ToList());

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Notifications.Count(n => !n.IsRead));

    public Task MarkAsReadAsync(Guid userId, Guid notificationId, DateTime readAt, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default) => Task.CompletedTask;

    public Task<NotificationPage> SearchAsync(NotificationQuery query, CancellationToken ct = default) =>
        Task.FromResult(new NotificationPage(Notifications, 1, 20, Notifications.Count));

    public Task<bool> SetReadAsync(Guid clientId, Guid userId, Guid notificationId, bool read, DateTime now, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> SetArchivedAsync(Guid clientId, Guid userId, Guid notificationId, bool archived, DateTime now, CancellationToken ct = default) => Task.FromResult(true);
    public Task<int> MarkAllAsReadAsync(Guid clientId, Guid userId, DateTime readAt, CancellationToken ct = default) => Task.FromResult(Notifications.Count);
    public Task<int> ArchiveReadAsync(Guid clientId, Guid userId, DateTime archivedAt, CancellationToken ct = default) => Task.FromResult(0);
}

internal sealed class OfflineSyncRepoMock : IOfflineSyncRepository
{
    public List<OfflineSyncQueueItem> Items { get; } = [];

    public Task EnqueueAsync(OfflineSyncQueueItem item, CancellationToken ct = default)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public Task<OfflineSyncQueueItem?> GetByIdAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.ClientId == clientId && i.UserId == userId && i.Id == id));

    public Task<IReadOnlyList<OfflineSyncQueueItem>> ListByUserAsync(Guid clientId, Guid userId, string? status = null, int limit = 50, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OfflineSyncQueueItem>>(Items.Where(i => i.ClientId == clientId && i.UserId == userId && (status == null || i.Status == status)).Take(limit).ToList());

    public Task UpdateStatusAsync(Guid clientId, Guid userId, Guid id, string status, string? errorMessage, string? conflictDetailsJson, DateTime? syncedAt, CancellationToken ct = default)
    {
        var item = Items.FirstOrDefault(i => i.ClientId == clientId && i.UserId == userId && i.Id == id);
        if (item is not null)
        {
            var idx = Items.IndexOf(item);
            Items[idx] = item with { Status = status, ErrorMessage = errorMessage, ConflictDetailsJson = conflictDetailsJson, SyncedAt = syncedAt };
        }
        return Task.CompletedTask;
    }

    public Task<int> CountPendingAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Items.Count(i => i.ClientId == clientId && i.UserId == userId && i.Status == "Pending"));
}

internal sealed class DataPortabilityRepoMock : IDataPortabilityRepository
{
    public List<DataExportRequestRecord> Exports { get; } = [];
    public List<DataImportBatchRecord> Imports { get; } = [];

    public Task RecordExportAsync(DataExportRequestRecord record, CancellationToken ct = default)
    {
        Exports.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DataExportRequestRecord>> ListExportsAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DataExportRequestRecord>>(Exports.Where(e => e.ClientId == clientId && e.UserId == userId).Take(limit).ToList());

    public Task RecordImportBatchAsync(DataImportBatchRecord record, CancellationToken ct = default)
    {
        Imports.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DataImportBatchRecord>> ListImportBatchesAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DataImportBatchRecord>>(Imports.Where(i => i.ClientId == clientId && i.UserId == userId).Take(limit).ToList());
}

internal sealed class GoalRepoMock : IUserGoalRepository
{
    public Task<IReadOnlyList<UserGoal>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserGoal>>([]);
    public Task<UserGoal?> GetAsync(Guid id, Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<UserGoal?>(null);
    public Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(0);
    public Task CreateAsync(UserGoal goal, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(UserGoal goal, CancellationToken ct = default) => Task.CompletedTask;
    public Task SetStatusAsync(Guid id, Guid clientId, Guid userId, string status, CancellationToken ct = default) => Task.CompletedTask;
    public Task LinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task UnlinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<Habit>> ListLinkedHabitsAsync(Guid goalId, Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Habit>>([]);
    public Task<IReadOnlyList<GoalTimelineEntry>> ListTimelineAsync(Guid goalId, Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GoalTimelineEntry>>([]);
}

internal sealed class ReviewRepoMock : IWeeklyReviewRepository
{
    public Task<WeeklyReview?> GetAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default) => Task.FromResult<WeeklyReview?>(null);
    public Task<WeeklyReview> CompleteAsync(WeeklyReview review, CancellationToken ct = default) => Task.FromResult(review);
}
