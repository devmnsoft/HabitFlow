namespace HabitFlow.Domain;

public static class LgpdRequestTypes
{
    public const string Export = "Export";
    public const string Deletion = "Deletion";
    public const string Anonymization = "Anonymization";
    public const string Rectification = "Rectification";

    public static readonly IReadOnlyList<string> All = [Export, Deletion, Anonymization, Rectification];
}

public static class LgpdRequestStatuses
{
    public const string Open = "Aberta";
    public const string InReview = "Em análise";
    public const string AwaitingConfirmation = "Aguardando confirmação";
    public const string Processed = "Processada";
    public const string Rejected = "Recusada";
    public const string Canceled = "Cancelada";

    public static readonly IReadOnlyList<string> All = [Open, InReview, AwaitingConfirmation, Processed, Rejected, Canceled];
}

public sealed record LgpdRequestRecord(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string RequestType,
    string Status,
    string? Reason,
    string? AdminNotes,
    Guid? ProcessedByUserId,
    DateTime? ProcessedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record ConsentRecord(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string ConsentType,
    bool Granted,
    string PolicyVersion,
    string? IpAddress,
    string? UserAgent,
    DateTime GrantedAt,
    DateTime? RevokedAt
);

public interface ILgpdGovernanceRepository
{
    Task CreateRequestAsync(LgpdRequestRecord request, CancellationToken ct = default);
    Task<LgpdRequestRecord?> GetRequestByIdAsync(Guid clientId, Guid requestId, CancellationToken ct = default);
    Task<IReadOnlyList<LgpdRequestRecord>> ListRequestsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<LgpdRequestRecord>> ListGlobalRequestsAsync(string? status = null, int limit = 50, CancellationToken ct = default);
    Task UpdateRequestStatusAsync(Guid requestId, string status, string? adminNotes, Guid processedByUserId, CancellationToken ct = default);
    Task RecordConsentAsync(ConsentRecord consent, CancellationToken ct = default);
    Task<IReadOnlyList<ConsentRecord>> ListConsentsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task RevokeConsentAsync(Guid clientId, Guid userId, string consentType, CancellationToken ct = default);
}
