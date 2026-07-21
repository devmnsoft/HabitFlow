using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class NotificationRepository(SqlExecutor db) : INotificationRepository
{
    public Task CreateAsync(Notification n, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.notifications(id,user_id,type,title,message,is_read,related_entity_type,related_entity_id,created_at,read_at) values(@Id,@UserId,@Type,@Title,@Message,@IsRead,@RelatedEntityType,@RelatedEntityId,@CreatedAt,@ReadAt)", n, ct);
    public async Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<Notification>("select id,user_id,type,title,message,is_read,related_entity_type,related_entity_id,created_at,read_at from habitflow.notifications where user_id=@userId and is_read=false order by created_at desc", new { userId }, ct)).ToList();
    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.notifications where user_id=@userId and is_read=false", new { userId }, ct)!;
    public Task MarkAsReadAsync(Guid userId, Guid notificationId, DateTime readAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.notifications set is_read=true, read_at=@readAt where id=@notificationId and user_id=@userId", new { userId, notificationId, readAt }, ct);
    public Task MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.notifications set is_read=true, read_at=@readAt where user_id=@userId and is_read=false", new { userId, readAt }, ct);
}
