using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class NotificationRepository(SqlExecutor db) : INotificationRepository
{
    private const string Columns = "id,user_id,type,title,message,severity,is_read,action_url,related_entity_type,related_entity_id,created_at,read_at";
    public Task CreateAsync(Notification n, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.notifications(id,user_id,type,title,message,severity,is_read,action_url,related_entity_type,related_entity_id,created_at,read_at) values(@Id,@UserId,@Type,@Title,@Message,@Severity,@IsRead,@ActionUrl,@RelatedEntityType,@RelatedEntityId,@CreatedAt,@ReadAt)", n, ct);
    public async Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<Notification>("select " + Columns + " from habitflow.notifications where user_id=@userId and is_read=false order by created_at desc", new { userId }, ct)).ToList();
    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.notifications where user_id=@userId and is_read=false", new { userId }, ct)!;
    public Task MarkAsReadAsync(Guid userId, Guid notificationId, DateTime readAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.notifications set is_read=true, read_at=@readAt where id=@notificationId and user_id=@userId", new { userId, notificationId, readAt }, ct);
    public Task MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.notifications set is_read=true, read_at=@readAt where user_id=@userId and is_read=false", new { userId, readAt }, ct);
    public async Task<NotificationPage> SearchAsync(NotificationQuery q, CancellationToken ct = default)
    {
        var where = " from habitflow.notifications n join habitflow.users u on u.id=n.user_id where n.user_id=@UserId and u.client_id=@ClientId and coalesce(n.is_archived,false)=@Archived and (@Filter='all' or (@Filter='unread' and not n.is_read) or (@Filter='read' and n.is_read)) and (@Category is null or coalesce(n.category,n.type)=@Category) and (@Search is null or n.title ilike '%'||@Search||'%' or n.message ilike '%'||@Search||'%')";
        var total = await db.QuerySingleOrDefaultAsync<int>("select count(*)" + where, q, ct);
        var offset = (Math.Max(q.Page,1)-1)*Math.Clamp(q.PageSize,1,50);
        var items = await db.QueryAsync<Notification>("select n.id,n.user_id,n.type,n.title,n.message,n.severity,n.is_read,n.action_url,n.related_entity_type,n.related_entity_id,n.created_at,n.read_at" + where + " order by n.created_at desc limit @limit offset @offset", new { q.UserId,q.ClientId,q.Filter,q.Category,q.Search,q.Archived,limit=Math.Clamp(q.PageSize,1,50),offset }, ct);
        return new NotificationPage(items.ToList(), Math.Max(q.Page,1), Math.Clamp(q.PageSize,1,50), total);
    }
    public async Task<bool> SetReadAsync(Guid clientId, Guid userId, Guid notificationId, bool read, DateTime now, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.notifications n set is_read=@read,read_at=case when @read then @now else null end from habitflow.users u where n.user_id=u.id and n.id=@notificationId and n.user_id=@userId and u.client_id=@clientId", new {clientId,userId,notificationId,read,now},ct)==1;
    public async Task<bool> SetArchivedAsync(Guid clientId, Guid userId, Guid notificationId, bool archived, DateTime now, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.notifications n set is_archived=@archived,archived_at=case when @archived then @now else null end from habitflow.users u where n.user_id=u.id and n.id=@notificationId and n.user_id=@userId and u.client_id=@clientId", new {clientId,userId,notificationId,archived,now},ct)==1;
    public Task<int> MarkAllAsReadAsync(Guid clientId, Guid userId, DateTime readAt, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.notifications n set is_read=true,read_at=@readAt from habitflow.users u where n.user_id=u.id and n.user_id=@userId and u.client_id=@clientId and n.is_read=false and coalesce(n.is_archived,false)=false", new {clientId,userId,readAt},ct);
    public Task<int> ArchiveReadAsync(Guid clientId, Guid userId, DateTime archivedAt, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.notifications n set is_archived=true,archived_at=@archivedAt from habitflow.users u where n.user_id=u.id and n.user_id=@userId and u.client_id=@clientId and n.is_read=true and coalesce(n.is_archived,false)=false", new {clientId,userId,archivedAt},ct);
}
