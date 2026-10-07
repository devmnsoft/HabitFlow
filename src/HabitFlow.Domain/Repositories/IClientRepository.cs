namespace HabitFlow.Domain;

public interface IClientRepository
{
    Task CreateAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> SearchAsync(string? search, ClientStatus? status, ClientPlan? plan, int offset, int pageSize, Guid? clientId = null, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetEnabledModulesAsync(Guid clientId, CancellationToken ct = default);
    Task RecordCommercialStatusChangeAsync(Guid clientId, string previousStatus, string newStatus, string? reason, Guid? changedBy, CancellationToken ct = default);
    Task<bool> DocumentExistsAsync(string documentNormalized, Guid? ignoreClientId = null, CancellationToken ct = default);
    Task<Client?> GetByDocumentAsync(string documentNormalized, CancellationToken ct = default);
    Task<IReadOnlyList<ClientUserSummary>> GetUsersAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<ClientUserSummary>> SearchUsersAsync(Guid clientId, string? search, string? role, string? accountStatus, int offset, int pageSize, CancellationToken ct = default);
    Task<int> CountUsersAsync(Guid clientId, string? search, string? role, string? accountStatus, CancellationToken ct = default);
    Task<ClientUserSummary?> GetUserByClientEmailAsync(Guid clientId, string normalizedEmail, CancellationToken ct = default);
    Task<ClientMetrics> GetMetricsAsync(Guid clientId, CancellationToken ct = default);
}
