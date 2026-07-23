namespace HabitFlow.Domain;

public sealed record ClientOnboarding(Guid Id, Guid ClientId, bool CompanyDataCompleted, bool BillingDataCompleted, bool FirstUserInvited, bool FirstHabitCreated, bool PlanReviewed, bool Completed, DateTime? CompletedAt, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record BillingCommunicationRule(Guid Id, string Code, string Name, string TriggerType, int DaysOffset, string Channel, string Title, string MessageTemplate, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ClientCommunication(Guid Id, Guid ClientId, Guid? UserId, Guid? InvoiceId, string Type, string Channel, string Title, string Message, string Status, DateTime? SentAt, DateTime? ReadAt, DateTime CreatedAt);
public sealed record JobExecutionLog(Guid Id, string JobName, string Status, DateTime StartedAt, DateTime? FinishedAt, long? DurationMs, int ProcessedCount, string? ErrorMessage, DateTime CreatedAt);
public sealed record CustomerHealthScore(Guid ClientId, int Score, string Status, IReadOnlyList<string> Signals);
public sealed record ClientCommunicationFilter(Guid? ClientId = null, string? Type = null, string? Status = null, DateTime? From = null, DateTime? To = null);
public sealed record OnboardingChecklistItem(string Text, bool Completed, string Icon, string ActionUrl, string HelpText);
