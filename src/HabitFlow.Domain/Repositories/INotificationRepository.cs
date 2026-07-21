namespace HabitFlow.Domain;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid userId, Guid notificationId, DateTime readAt, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default);
}
