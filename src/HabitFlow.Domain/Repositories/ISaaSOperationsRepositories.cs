namespace HabitFlow.Domain;

public interface IClientOnboardingRepository
{
    Task<ClientOnboarding> GetOrCreateAsync(Guid clientId, CancellationToken ct = default);
    Task UpdateStepAsync(Guid clientId, string step, bool completed, CancellationToken ct = default);
}

public interface IClientCommunicationRepository
{
    Task CreateAsync(ClientCommunication communication, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid clientId, Guid? invoiceId, string type, string channel, CancellationToken ct = default);
    Task<IReadOnlyList<ClientCommunication>> ListByClientAsync(Guid clientId, ClientCommunicationFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ClientCommunication>> ListAllAsync(ClientCommunicationFilter filter, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid clientId, Guid communicationId, DateTime readAt, CancellationToken ct = default);
}

public interface IBillingCommunicationRuleRepository
{
    Task<IReadOnlyList<BillingCommunicationRule>> ListActiveAsync(CancellationToken ct = default);
}

public interface IJobExecutionLogRepository
{
    Task<Guid> StartAsync(string jobName, CancellationToken ct = default);
    Task FinishAsync(Guid id, string status, int processedCount, string? errorMessage, CancellationToken ct = default);
}
