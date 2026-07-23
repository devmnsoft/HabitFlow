using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ClientOnboardingRepository(SqlExecutor db) : IClientOnboardingRepository
{
    private const string Columns = "id,client_id,company_data_completed,billing_data_completed,first_user_invited,first_habit_created,plan_reviewed,completed,completed_at,created_at,updated_at";
    public async Task<ClientOnboarding> GetOrCreateAsync(Guid clientId, CancellationToken ct = default)
    {
        var existing = await db.QuerySingleOrDefaultAsync<ClientOnboarding>("select " + Columns + " from habitflow.client_onboarding where client_id=@clientId", new { clientId }, ct);
        if (existing is not null) return existing;
        var onboarding = new ClientOnboarding(Guid.NewGuid(), clientId, false, false, false, false, false, false, null, DateTime.UtcNow, DateTime.UtcNow);
        await db.ExecuteAsync("insert into habitflow.client_onboarding(id,client_id,company_data_completed,billing_data_completed,first_user_invited,first_habit_created,plan_reviewed,completed,completed_at,created_at,updated_at) values(@Id,@ClientId,@CompanyDataCompleted,@BillingDataCompleted,@FirstUserInvited,@FirstHabitCreated,@PlanReviewed,@Completed,@CompletedAt,@CreatedAt,@UpdatedAt)", onboarding, ct);
        return onboarding;
    }
    public Task UpdateStepAsync(Guid clientId, string step, bool completed, CancellationToken ct = default)
    {
        var allowed = new HashSet<string> { "company_data_completed", "billing_data_completed", "first_user_invited", "first_habit_created", "plan_reviewed", "completed" };
        if (!allowed.Contains(step)) throw new ArgumentOutOfRangeException(nameof(step));
        var completedAt = step == "completed" && completed ? ", completed_at=now()" : string.Empty;
        return db.ExecuteAsync($"update habitflow.client_onboarding set {step}=@completed, updated_at=now(){completedAt} where client_id=@clientId", new { clientId, completed }, ct);
    }
}

public sealed class ClientCommunicationRepository(SqlExecutor db) : IClientCommunicationRepository
{
    private const string Columns = "id,client_id,user_id,invoice_id,type,channel,title,message,status,sent_at,read_at,created_at";
    public Task CreateAsync(ClientCommunication c, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.client_communications(id,client_id,user_id,invoice_id,type,channel,title,message,status,sent_at,read_at,created_at) values(@Id,@ClientId,@UserId,@InvoiceId,@Type,@Channel,@Title,@Message,@Status,@SentAt,@ReadAt,@CreatedAt)", c, ct);
    public Task<bool> ExistsAsync(Guid clientId, Guid? invoiceId, string type, string channel, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<bool>("select exists(select 1 from habitflow.client_communications where client_id=@clientId and invoice_id is not distinct from @invoiceId and type=@type and channel=@channel and status <> 'Canceled')", new { clientId, invoiceId, type, channel }, ct)!;
    public async Task<IReadOnlyList<ClientCommunication>> ListByClientAsync(Guid clientId, ClientCommunicationFilter filter, CancellationToken ct = default) => (await db.QueryAsync<ClientCommunication>("select " + Columns + " from habitflow.client_communications where client_id=@clientId and (@type is null or type=@type) and (@status is null or status=@status) order by created_at desc", new { clientId, type = filter.Type, status = filter.Status }, ct)).ToList();
    public async Task<IReadOnlyList<ClientCommunication>> ListAllAsync(ClientCommunicationFilter filter, CancellationToken ct = default) => (await db.QueryAsync<ClientCommunication>("select " + Columns + " from habitflow.client_communications where (@clientId is null or client_id=@clientId) and (@type is null or type=@type) and (@status is null or status=@status) order by created_at desc limit 500", new { clientId = filter.ClientId, type = filter.Type, status = filter.Status }, ct)).ToList();
    public Task MarkAsReadAsync(Guid clientId, Guid communicationId, DateTime readAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.client_communications set status='Read', read_at=@readAt where id=@communicationId and client_id=@clientId", new { clientId, communicationId, readAt }, ct);
}

public sealed class BillingCommunicationRuleRepository(SqlExecutor db) : IBillingCommunicationRuleRepository
{
    public async Task<IReadOnlyList<BillingCommunicationRule>> ListActiveAsync(CancellationToken ct = default) => (await db.QueryAsync<BillingCommunicationRule>("select id,code,name,trigger_type,days_offset,channel,title,message_template,is_active,created_at,updated_at from habitflow.billing_communication_rules where is_active=true order by trigger_type,days_offset", null, ct)).ToList();
}

public sealed class JobExecutionLogRepository(SqlExecutor db) : IJobExecutionLogRepository
{
    public async Task<Guid> StartAsync(string jobName, CancellationToken ct = default) { var id = Guid.NewGuid(); await db.ExecuteAsync("insert into habitflow.job_execution_logs(id,job_name,status,started_at,created_at) values(@id,@jobName,'Running',now(),now())", new { id, jobName }, ct); return id; }
    public Task FinishAsync(Guid id, string status, int processedCount, string? errorMessage, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.job_execution_logs set status=@status, finished_at=now(), duration_ms=(extract(epoch from (now()-started_at))*1000)::bigint, processed_count=@processedCount, error_message=@errorMessage where id=@id", new { id, status, processedCount, errorMessage }, ct);
}
