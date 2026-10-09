using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public static class NotificationEventTypes
{
    public const string HabitReminder = "habit_reminder";
    public const string WeeklyReviewAvailable = "weekly_review_available";
    public const string ConsistencyDrop = "consistency_drop";
    public const string GoalCompleted = "goal_completed";
    public const string TrialExpiring = "trial_expiring";
    public const string TrialExpired = "trial_expired";
    public const string PaymentPending = "payment_pending";
    public const string SubscriptionActive = "subscription_active";
    public const string TicketReplied = "ticket_replied";
    public const string InviteReceived = "invite_received";
    public const string PlanLimitReached = "plan_limit_reached";
    public const string AiUnavailable = "ai_unavailable";
    public const string OfflineSyncError = "offline_sync_error";
}

public sealed class NotificationEventDispatcher(
    INotificationRepository notifications,
    PushNotificationPreferenceService preferences,
    PushNotificationService push,
    AuditService audit,
    ILogger<NotificationEventDispatcher> logger)
{
    public async Task<Result> DispatchAsync(
        Guid clientId,
        Guid userId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var prefs = await preferences.GetAsync(clientId, userId, ct);

        // Verifica quiet hours se configurado
        if (prefs is { QuietStart: not null, QuietEnd: not null })
        {
            var now = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-3)); // Horário de Brasília por padrão
            if (IsInQuietHours(now, prefs.QuietStart.Value, prefs.QuietEnd.Value))
            {
                logger.LogInformation("Notificação {Type} suprimida durante quiet hours para {UserId}", type, userId);
                return Result.Success();
            }
        }

        // Verifica se a notificação interna está permitida
        if (prefs is not null && !prefs.InternalEnabled)
        {
            logger.LogInformation("Notificações internas desativadas pelo usuário {UserId}", userId);
            return Result.Success();
        }

        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            type,
            title,
            message,
            "Info",
            false,
            actionUrl,
            null,
            relatedEntityId,
            DateTime.UtcNow,
            null
        );

        await notifications.CreateAsync(notification, ct);
        await audit.LogAsync("notification.created", "Notificação de evento criada", AuditSeverity.Info, userId, null, new { notification.Id, type, title, clientId }, ct);

        // Se o usuário tiver consentimento para push ativado, tenta enviar push
        if (prefs is { PushEnabled: true })
        {
            try
            {
                await push.SendSafeReminderAsync(clientId, userId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push não pôde ser entregue para {UserId}", userId);
            }
        }

        return Result.Success();
    }

    public static bool IsInQuietHours(TimeOnly current, TimeOnly start, TimeOnly end)
    {
        if (start < end)
        {
            return current >= start && current <= end;
        }
        // Exemplo: 22:00 até 07:00 (cruza a meia-noite)
        return current >= start || current <= end;
    }
}
