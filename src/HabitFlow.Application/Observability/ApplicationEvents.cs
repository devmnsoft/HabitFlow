using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public static class ApplicationEvents
{
    public static readonly EventId AuditIssueFound = new(618700, "audit.issue_found");
    public static readonly EventId AuditIssueFixed = new(618701, "audit.issue_fixed");
    public static readonly EventId ValidationFailed = new(618702, "validation.failed");
    public static readonly EventId HabitOperationFailed = new(618703, "habit.operation_failed");
    public static readonly EventId GoalOperationFailed = new(618704, "goal.operation_failed");
    public static readonly EventId RoutineOperationFailed = new(618705, "routine.operation_failed");
    public static readonly EventId NotificationOperationFailed = new(618706, "notification.operation_failed");
    public static readonly EventId TenantAccessDenied = new(618707, "tenant.access_denied");
    public static readonly EventId SystemHealthChecked = new(618708, "system.health_checked");
    public static readonly EventId HabitCreated = new(617610, "habit.created");
    public static readonly EventId HabitUpdated = new(617611, "habit.updated");
    public static readonly EventId HabitDeleted = new(617612, "habit.deleted");
    public static readonly EventId ReminderCreated = new(617620, "reminder.created");
    public static readonly EventId ReminderPaused = new(617621, "reminder.paused");
    public static readonly EventId ReminderResumed = new(617622, "reminder.resumed");
    public static readonly EventId ReminderDispatchFailed = new(617623, "reminder.dispatch.failed");
    public static readonly EventId SupportTicketCreated = new(617630, "support.ticket.created");
    public static readonly EventId SupportTicketUpdated = new(617631, "support.ticket.updated");
    public static readonly EventId SupportTicketClosed = new(617632, "support.ticket.closed");
    public static readonly EventId AssistantMessageReceived = new(617640, "assistant.message.received");
    public static readonly EventId AssistantMessageBlocked = new(617641, "assistant.message.blocked");
    public static readonly EventId AssistantMessageAnswered = new(617642, "assistant.message.answered");
    public static readonly EventId AssistantDisabled = new(618200, "assistant.disabled");
    public static readonly EventId AssistantContextBuilt = new(618201, "assistant.context.built");
    public static readonly EventId AssistantResponseGenerated = new(618202, "assistant.response.generated");
    public static readonly EventId AssistantSafetyBlocked = new(618203, "assistant.safety.blocked");
    public static readonly EventId AssistantProviderTimeout = new(618204, "assistant.provider.timeout");
    public static readonly EventId AssistantProviderError = new(618205, "assistant.provider.error");
    public static readonly EventId BillingPlanViewed = new(617650, "billing.plan.viewed");
    public static readonly EventId BillingCheckoutStarted = new(617651, "billing.checkout.started");
    public static readonly EventId AuthLoginFailed = new(617660, "auth.login.failed");
    public static readonly EventId AuthLoginSucceeded = new(617661, "auth.login.succeeded");
    public static readonly EventId SystemUnhandled = new(617670, "system.error.unhandled");
    public static readonly EventId DatabaseUnavailable = new(617671, "system.database.unavailable");
    public static readonly EventId HealthFailed = new(617672, "system.health.failed");
}
