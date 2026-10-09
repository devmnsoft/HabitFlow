using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class OfflineSyncRepository(SqlExecutor db) : IOfflineSyncRepository
{
    const string Columns = "id,client_id,user_id,action_type,entity_id,payload::text as payload_json,status,error_message,conflict_details::text as conflict_details_json,client_created_at,synced_at,created_at";

    public Task EnqueueAsync(OfflineSyncQueueItem item, CancellationToken ct = default) =>
        db.ExecuteAsync(
            "insert into habitflow.offline_sync_queue(id,client_id,user_id,action_type,entity_id,payload,status,error_message,conflict_details,client_created_at,synced_at,created_at) " +
            "values(@Id,@ClientId,@UserId,@ActionType,@EntityId,cast(@PayloadJson as jsonb),@Status,@ErrorMessage,case when @ConflictDetailsJson is null then null else cast(@ConflictDetailsJson as jsonb) end,@ClientCreatedAt,@SyncedAt,@CreatedAt) " +
            "on conflict(id) do update set status=excluded.status, error_message=excluded.error_message, conflict_details=excluded.conflict_details, synced_at=excluded.synced_at",
            item, ct);

    public Task<OfflineSyncQueueItem?> GetByIdAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<OfflineSyncQueueItem>($"select {Columns} from habitflow.offline_sync_queue where id=@id and client_id=@clientId and user_id=@userId", new { id, clientId, userId }, ct);

    public async Task<IReadOnlyList<OfflineSyncQueueItem>> ListByUserAsync(Guid clientId, Guid userId, string? status = null, int limit = 50, CancellationToken ct = default)
    {
        var sql = string.IsNullOrWhiteSpace(status)
            ? $"select {Columns} from habitflow.offline_sync_queue where client_id=@clientId and user_id=@userId order by created_at desc limit @limit"
            : $"select {Columns} from habitflow.offline_sync_queue where client_id=@clientId and user_id=@userId and status=@status order by created_at desc limit @limit";
        return (await db.QueryAsync<OfflineSyncQueueItem>(sql, new { clientId, userId, status, limit }, ct)).ToList();
    }

    public Task UpdateStatusAsync(Guid clientId, Guid userId, Guid id, string status, string? errorMessage, string? conflictDetailsJson, DateTime? syncedAt, CancellationToken ct = default) =>
        db.ExecuteAsync(
            "update habitflow.offline_sync_queue set status=@status, error_message=@errorMessage, conflict_details=(case when @conflictDetailsJson is null then null else cast(@conflictDetailsJson as jsonb) end), synced_at=@syncedAt where id=@id and client_id=@clientId and user_id=@userId",
            new { id, clientId, userId, status, errorMessage, conflictDetailsJson, syncedAt }, ct);

    public Task<int> CountPendingAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.offline_sync_queue where client_id=@clientId and user_id=@userId and status='Pending'", new { clientId, userId }, ct);
}
