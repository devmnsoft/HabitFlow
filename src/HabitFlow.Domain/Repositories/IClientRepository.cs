namespace HabitFlow.Domain;

public interface IClientRepository
{
    Task CreateAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> SearchAsync(string? search, ClientStatus? status, ClientPlan? plan, int offset, int pageSize, CancellationToken ct = default);
    Task<bool> DocumentExistsAsync(string document, Guid? exceptId = null, CancellationToken ct = default);
    Task<IReadOnlyList<ClientUserSummary>> GetUsersAsync(Guid clientId, CancellationToken ct = default);
    Task<ClientMetrics> GetMetricsAsync(Guid clientId, CancellationToken ct = default);
}
