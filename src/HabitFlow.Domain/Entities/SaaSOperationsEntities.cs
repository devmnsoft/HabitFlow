namespace HabitFlow.Domain;

public sealed record ClientOnboarding(Guid Id, Guid ClientId, bool CompanyDataCompleted, bool BillingDataCompleted, bool FirstUserInvited, bool FirstHabitCreated, bool PlanReviewed, bool Completed, DateTime? CompletedAt, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record BillingCommunicationRule(Guid Id, string Code, string Name, string TriggerType, int DaysOffset, string Channel, string Title, string MessageTemplate, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ClientCommunication(Guid Id, Guid ClientId, Guid? UserId, Guid? InvoiceId, string Type, string Channel, string Title, string Message, string Status, DateTime? SentAt, DateTime? ReadAt, DateTime CreatedAt);
public sealed record JobExecutionLog(Guid Id, string JobName, string Status, DateTime StartedAt, DateTime? FinishedAt, long? DurationMs, int ProcessedCount, string? ErrorMessage, DateTime CreatedAt);
public sealed record CustomerHealthScore(Guid ClientId, int Score, string Status, IReadOnlyList<string> Signals);
public sealed record ClientCommunicationFilter(Guid? ClientId = null, string? Type = null, string? Status = null, DateTime? From = null, DateTime? To = null);
public sealed record OnboardingChecklistItem(string Text, bool Completed, string Icon, string ActionUrl, string HelpText);

public sealed record SuperAdminPlanRow(string Code, string Name, decimal? PriceMonthly, decimal? PriceYearly, int? HabitLimit, bool ReportsEnabled, bool AdvancedReportsEnabled, bool IsActive, bool IsPublic, int ClientsUsing, decimal EstimatedRevenue);
public sealed record SuperAdminSubscriptionRow(Guid Id, Guid ClientId, string ClientName, string PlanCode, string Status, string? BillingCycle, DateTime? CurrentPeriodStart, DateTime? CurrentPeriodEnd, DateTime? TrialEndsAt, DateTime? CanceledAt, DateOnly? NextDueDate);
public sealed record SuperAdminPaymentRow(Guid Id, Guid ClientId, string ClientName, string? InvoiceNumber, decimal Amount, DateOnly DueDate, string Method, string Status, DateTime? PaidAt, string? CheckoutUrl, string? ProviderPaymentId);
public sealed record SuperAdminAuditRow(DateTime CreatedAt, string ActorEmail, string Action, string TargetType, Guid? TargetId, string? Reason, string? Metadata);
public sealed record SchemaMigrationStatus(string Id, string Name, bool Applied, DateTime? AppliedAt);
public sealed record SystemHealthStatus(bool DatabaseOk, IReadOnlyList<SchemaMigrationStatus> Migrations, IReadOnlyList<string> MissingTables, IReadOnlyList<string> MissingIndexes, IReadOnlyList<string> MissingConstraints, int PlansCount, int HabitTemplatesCount, bool MercadoPagoConfigured, string EnvironmentName, string ApplicationVersion, string Urls, bool DockerRequired);

public sealed record RegistrationQualitySummary(int TodayRegistrations, int MonthRegistrations, int NaturalPersonClients, int LegalPersonClients, int ClientsWithoutDocument, int ClientsWithInvalidDocument, int ClientsWithoutAdmin, int UsersWithoutClient, int FreeClients, int PremiumClients, int ClientsWithBlockedBenefits);
public sealed record RegistrationQualityRow(DateTime CreatedAt, Guid ClientId, string PersonType, string Name, string? Document, string? Email, string Plan, string BenefitsStatus, string PaymentStatus, string? AdminEmail);
public sealed record RegistrationQualityReport(RegistrationQualitySummary Summary, IReadOnlyList<RegistrationQualityRow> Recent);
