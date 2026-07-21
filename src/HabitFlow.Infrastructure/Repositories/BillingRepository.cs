using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class BillingRepository(SqlExecutor db) : IBillingRepository
{
    public Task AddAsync(BillingEvent billingEvent, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.billing_events(id,user_id,provider,event_type,plan,status,amount,metadata,created_at) values(@Id,@UserId,@Provider,@EventType,@Plan::text,@Status,@Amount,@Metadata::jsonb,@CreatedAt)", billingEvent, ct);
}
