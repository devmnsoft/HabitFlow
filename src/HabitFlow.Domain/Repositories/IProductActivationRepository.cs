namespace HabitFlow.Domain;

public interface IProductActivationRepository
{
    Task<IReadOnlyList<TenantActivationRow>> ListTenantsAsync(HomologationQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ImplantationOverride>> ListOverridesAsync(Guid clientId, CancellationToken ct = default);
    Task SaveOverrideAsync(Guid clientId, Guid userId, string stepCode, string status, string? reason, CancellationToken ct = default);
    Task RecordNotificationEventAsync(Guid clientId, Guid? userId, string eventCode, string channel, string status, CancellationToken ct = default);
    Task RecordCustomerSuccessAsync(Guid clientId, int? score, string status, CancellationToken ct = default);
}
