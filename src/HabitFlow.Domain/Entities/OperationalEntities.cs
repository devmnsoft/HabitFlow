using HabitFlow.Shared;

namespace HabitFlow.Domain;

public static class TenantHealthStatus
{
    public const string Healthy = "Healthy";
    public const string Attention = "Attention";
    public const string AtRisk = "AtRisk";
    public const string Blocked = "Blocked";
    public const string TrialEnding = "TrialEnding";
    public const string PaymentIssue = "PaymentIssue";

    public static readonly string[] All = [Healthy, Attention, AtRisk, Blocked, TrialEnding, PaymentIssue];
}

public sealed record TenantCustomerSuccessIndicator(
    Guid ClientId,
    string ClientName,
    string ClientStatus,
    string Plan,
    int? TrialRemainingDays,
    string SubscriptionStatus,
    string PaymentStatus,
    int ActiveUsers,
    int InactiveUsers,
    int HabitsCount,
    int Completions7Days,
    int Completions30Days,
    int AiRequestsCount,
    int ReportsGeneratedCount,
    int OpenTicketsCount,
    DateTime? LastActivityAt,
    bool ChurnRisk,
    string OnboardingStep,
    int OnboardingCompletedPercent,
    bool LimitBlocked,
    IReadOnlyList<string> CommercialPendencies,
    int HealthScore,
    string HealthCategory,
    IReadOnlyList<string> RiskFactors
);

public sealed record OperationalIncident(
    Guid Id,
    string Title,
    string Description,
    string Severity, // Info, Minor, Major, Critical
    string Status,   // Investigating, Identified, Monitoring, Resolved, Canceled
    string Impact,
    int AffectedTenantsCount,
    DateTime StartsAt,
    DateTime? EstimatedResolutionAt,
    DateTime? ResolvedAt,
    DateTime? CanceledAt,
    string? ResponsibleUserId,
    string? ResponsibleName,
    bool CommunicationSent,
    string? CommunicationNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record OperationalIncidentFilter(
    string? Status = null,
    string? Severity = null,
    string? Search = null
);

public sealed record IncidentTenant(
    Guid Id,
    Guid IncidentId,
    Guid ClientId,
    string? ImpactSummary,
    bool Notified,
    DateTime CreatedAt
);

public sealed record CustomerSuccessNote(
    Guid Id,
    Guid ClientId,
    string AuthorUserId,
    string AuthorName,
    string NoteType,
    string Content,
    bool IsRiskFlag,
    DateTime CreatedAt
);

public sealed record OperationalAuditEvent(
    Guid Id,
    string EventName,
    string CorrelationId,
    Guid? ClientId,
    string? ExecutorUserId,
    string? ExecutorEmail,
    string Severity,
    string Status,
    string PayloadJson,
    DateTime OccurredAt
);

public interface IOperationalIncidentRepository
{
    Task<IReadOnlyList<OperationalIncident>> ListIncidentsAsync(OperationalIncidentFilter filter, CancellationToken ct = default);
    Task<OperationalIncident?> GetIncidentByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateIncidentAsync(OperationalIncident incident, CancellationToken ct = default);
    Task UpdateIncidentAsync(OperationalIncident incident, CancellationToken ct = default);
    Task RecordIncidentAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default);
    Task<IReadOnlyList<IncidentTenant>> ListIncidentTenantsAsync(Guid incidentId, CancellationToken ct = default);
    Task LinkIncidentTenantsAsync(Guid incidentId, IEnumerable<Guid> clientIds, string? impactSummary, CancellationToken ct = default);
}
