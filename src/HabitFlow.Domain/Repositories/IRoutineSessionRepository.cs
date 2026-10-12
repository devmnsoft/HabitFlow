namespace HabitFlow.Domain;

public interface IRoutineSessionRepository
{
    Task<RoutineSession?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, string idempotencyKey, CancellationToken ct = default);
    Task<RoutineSessionDetails?> GetAsync(Guid clientId, Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<RoutineSessionDetails?> GetActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<RoutineSessionSummary>> ListHistoryAsync(Guid clientId, Guid userId, DateOnly? from, DateOnly? to, string? status, int offset, int limit, CancellationToken ct = default);
    Task CreateAsync(RoutineSession session, IReadOnlyList<RoutineSessionItem> items, string correlationId, CancellationToken ct = default);
    Task<RoutineSession?> TransitionAsync(Guid clientId, Guid userId, Guid sessionId, string fromStatus, string toStatus, string command, string idempotencyKey, string correlationId, DateTime now, CancellationToken ct = default);
    Task<RoutineSessionItem?> UpdateItemAsync(Guid clientId, Guid userId, Guid sessionId, Guid itemId, string status, decimal? recordedQuantity, string? recordedUnit, int? effectiveMinutes, Guid? completionId, bool skipped, string? skipReason, DateTime now, CancellationToken ct = default);
    Task SetClosureAsync(Guid clientId, Guid userId, Guid sessionId, string? reflection, string summary, DateTime now, CancellationToken ct = default);
}
