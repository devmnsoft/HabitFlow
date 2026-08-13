namespace HabitFlow.Domain;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid userId, Guid notificationId, DateTime readAt, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default);
    Task<NotificationPage> SearchAsync(NotificationQuery query, CancellationToken ct = default);
    Task<bool> SetReadAsync(Guid clientId, Guid userId, Guid notificationId, bool read, DateTime now, CancellationToken ct = default);
    Task<bool> SetArchivedAsync(Guid clientId, Guid userId, Guid notificationId, bool archived, DateTime now, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid clientId, Guid userId, DateTime readAt, CancellationToken ct = default);
    Task<int> ArchiveReadAsync(Guid clientId, Guid userId, DateTime archivedAt, CancellationToken ct = default);
}

public sealed record NotificationQuery(Guid ClientId, Guid UserId, string Filter = "all", string? Category = null,
    string? Search = null, int Page = 1, int PageSize = 20, bool Archived = false);
public sealed record NotificationPage(IReadOnlyList<Notification> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}
