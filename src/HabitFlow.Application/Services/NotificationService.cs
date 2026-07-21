using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class NotificationService(INotificationRepository notifications, AuditService audit, ILogger<NotificationService> logger)
{
    public async Task<Result> CreateAsync(Guid userId, string type, string title, string message, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        try
        {
            await notifications.CreateAsync(new Notification(Guid.NewGuid(), userId, type, title, message, false, relatedEntityType, relatedEntityId, DateTime.UtcNow, null), ct);
            await audit.LogAsync("notification_created", "Notificação criada", AuditSeverity.Info, userId, null, new { type, relatedEntityType }, ct);
            return Result.Success();
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar notificação para {UserId}", userId); return Result.Failure("notification.create_error", "Não foi possível criar a notificação."); }
    }
    public Task<IReadOnlyList<Notification>> ListUnreadAsync(Guid userId, CancellationToken ct = default) => notifications.ListUnreadAsync(userId, ct);
    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) => notifications.CountUnreadAsync(userId, ct);
    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default) { await notifications.MarkAsReadAsync(userId, notificationId, DateTime.UtcNow, ct); await audit.LogAsync("notification_read", "Notificação lida", AuditSeverity.Info, userId, null, new { notificationId }, ct); }
    public Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default) => notifications.MarkAllAsReadAsync(userId, DateTime.UtcNow, ct);
}
