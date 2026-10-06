namespace HabitFlow.Domain;

public interface IBillingEventLogRepository
{
    Task LogAsync(Guid? clientId, Guid? userId, string eventCode, string status, string? provider, string? planCode, string? sanitizedMetadataJson, Guid correlationId, CancellationToken ct = default);
}
