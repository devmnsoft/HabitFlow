namespace HabitFlow.Domain;

public interface IOfflineSyncRepository
{
    Task EnqueueAsync(OfflineSyncQueueItem item, CancellationToken ct = default);
    Task<OfflineSyncQueueItem?> GetByIdAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OfflineSyncQueueItem>> ListByUserAsync(Guid clientId, Guid userId, string? status = null, int limit = 50, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid clientId, Guid userId, Guid id, string status, string? errorMessage, string? conflictDetailsJson, DateTime? syncedAt, CancellationToken ct = default);
    Task<int> CountPendingAsync(Guid clientId, Guid userId, CancellationToken ct = default);
}
