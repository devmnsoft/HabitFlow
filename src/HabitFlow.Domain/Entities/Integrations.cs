namespace HabitFlow.Domain;

public sealed record ApiKeyRecord(Guid Id, Guid ClientId, Guid UserId, string Name, string KeyPrefix,
    string KeyHash, string[] Scopes, DateTime CreatedAt, DateTime? LastUsedAt, DateTime? RevokedAt);

public sealed record IntegrationWebhook(Guid Id, Guid ClientId, Guid UserId, string Name, string Url,
    string[] Events, string SecretCiphertext, bool Enabled, DateTime CreatedAt, DateTime? LastSuccessAt,
    int ConsecutiveFailures = 0, DateTime? LastFailedAt = null, bool IsPaused = false);

public sealed record WebhookDeliveryAttemptRecord(Guid Id, Guid WebhookId, Guid ClientId, Guid EventId,
    string EventName, short Attempt, string Status, int? ResponseCode, string? ResponseBody, string? ErrorMessage,
    int? DurationMs, DateTime CreatedAt);

public sealed record CalendarFeed(Guid Id, Guid ClientId, Guid UserId, string TokenHash, bool Enabled,
    bool IncludeHabits, bool IncludeRoutines, DateTime CreatedAt, DateTime? LastUsedAt);

public static class WebhookEvents
{
    public const string HabitCreated = "habit.created";
    public const string HabitCompleted = "habit.completed";
    public const string CheckinCreated = "checkin.created";
    public const string GoalCompleted = "goal.completed";
    public const string WeeklyReviewGenerated = "weekly_review.generated";
    public const string SubscriptionUpdated = "subscription.updated";
    public const string TrialExpiring = "trial.expiring";
    public const string TrialExpired = "trial.expired";
    public const string PaymentPending = "payment.pending";
    public const string PaymentApproved = "payment.approved";
    public const string SupportTicketCreated = "support.ticket.created";
    public const string SupportTicketUpdated = "support.ticket.updated";

    public static readonly IReadOnlyList<string> All =
    [
        HabitCreated, HabitCompleted, CheckinCreated, GoalCompleted,
        WeeklyReviewGenerated, SubscriptionUpdated, TrialExpiring, TrialExpired,
        PaymentPending, PaymentApproved, SupportTicketCreated, SupportTicketUpdated
    ];
}

public static class IntegrationScopes
{
    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "habits.read", "habits.write", "goals.read", "goals.write", "routines.read", "checkins.write",
        "notifications.read", "profile.read", "reports.read", "templates.read", "webhooks.manage",
        "habits:read", "habits:write", "goals:read", "goals:write", "routines:read", "checkins:write",
        "notifications:read", "profile:read", "reports:read", "templates:read", "webhooks:manage"
    };

    public static bool HasScope(IEnumerable<string>? userScopes, string requiredScope)
    {
        if (userScopes is null) return false;
        var normalizedRequired = requiredScope.Replace(':', '.');
        return userScopes.Any(s => s.Replace(':', '.').Equals(normalizedRequired, StringComparison.OrdinalIgnoreCase));
    }
}
