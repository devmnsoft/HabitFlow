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

public interface ISuperAdminOperationalRepository
{
    Task<IReadOnlyList<SuperAdminPlanRow>> ListPlansAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SuperAdminSubscriptionRow>> ListSubscriptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SuperAdminPaymentRow>> ListPaymentsAsync(string? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<SuperAdminAuditRow>> ListAuditAsync(CancellationToken ct = default);
    Task ChangeClientPlanAsync(Guid clientId, string planCode, string reason, string actorEmail, CancellationToken ct = default);
    Task MarkInvoicePaidAsync(Guid invoiceId, string reason, string actorEmail, CancellationToken ct = default);
    Task MarkInvoiceOverdueAsync(Guid invoiceId, string reason, string actorEmail, CancellationToken ct = default);
    Task CancelSubscriptionAsync(Guid subscriptionId, string reason, string actorEmail, CancellationToken ct = default);
    Task ReactivateSubscriptionAsync(Guid subscriptionId, string reason, string actorEmail, CancellationToken ct = default);
    Task<IReadOnlyList<SchemaMigrationStatus>> ListAppliedMigrationsAsync(CancellationToken ct = default);
    Task<SystemHealthStatus> BuildSystemHealthAsync(IReadOnlyList<SchemaMigrationStatus> expected, CancellationToken ct = default);
}
