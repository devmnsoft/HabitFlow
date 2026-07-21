namespace HabitFlow.Domain;

public sealed record Plan(Guid Id, string Code, string Name, string? Description, decimal? PriceMonthly, decimal? PriceYearly, string Currency, int? HabitLimit, bool ReportsEnabled, bool AdvancedReportsEnabled, bool ChallengesEnabled, bool IsActive, bool IsPublic, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record Subscription(Guid Id, Guid UserId, string PlanCode, SubscriptionStatus Status, BillingCycle? BillingCycle, PaymentProvider Provider, string? ProviderCustomerId, string? ProviderSubscriptionId, string? ProviderPaymentId, string? CheckoutUrl, DateTime? CurrentPeriodStart, DateTime? CurrentPeriodEnd, DateTime? TrialEndsAt, DateTime? CanceledAt, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record PaymentTransaction(Guid Id, Guid? UserId, Guid? SubscriptionId, PaymentProvider Provider, string? ProviderPaymentId, string? ProviderPreferenceId, string? EventType, PaymentStatus Status, decimal? Amount, string Currency, string? RawStatus, string? SanitizedMetadata, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record PaymentWebhookEvent(Guid Id, PaymentProvider Provider, string? EventId, string? EventType, string Status, DateTime ReceivedAt, DateTime? ProcessedAt, Guid? UserId, Guid? SubscriptionId, Guid? PaymentTransactionId, string? SanitizedPayload, string? ProcessingError);
public sealed record PaymentAuditLog(Guid Id, Guid? UserId, Guid? SubscriptionId, string Action, string Message, string Severity, string? Metadata, DateTime CreatedAt);

public sealed record CheckoutRequest(Guid UserId, string UserEmail, string UserName, string PlanCode, BillingCycle BillingCycle);
public sealed record CheckoutPreference(string CheckoutUrl, string? PreferenceId);
public sealed record ProviderPayment(string ProviderPaymentId, string? ExternalReference, string RawStatus, PaymentStatus Status, decimal? Amount, string Currency, string? PreferenceId);
public sealed record FinancialDashboard(decimal ApprovedRevenue, decimal PendingRevenue, decimal RejectedRevenue, int ActiveSubscriptions, int PendingSubscriptions, int CanceledSubscriptions, decimal EstimatedMrr, decimal EstimatedArr, IReadOnlyList<PaymentTransaction> LatestTransactions, IReadOnlyList<PaymentWebhookEvent> LatestWebhooks);
