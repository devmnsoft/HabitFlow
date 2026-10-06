using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class BillingStatusRepository(SqlExecutor db) : IBillingStatusRepository
{
    public Task<int> MarkOverdueInvoicesAsync(DateOnly today, CancellationToken ct = default) => db.ExecuteAsync(@"
update habitflow.client_invoices i
set status = 'Overdue', updated_at = now()
where i.status in ('Pending','Open') and i.due_date < @today", new { today }, ct);

    public Task<int> BlockBenefitsAfterGracePeriodAsync(DateOnly today, int gracePeriodDays, CancellationToken ct = default) => db.ExecuteAsync(@"
update habitflow.clients c
set payment_status = 'Overdue',
    subscription_status = 'PastDue',
    benefits_status = case when c.plan = 'Enterprise' then 'EnterpriseBlocked' else 'PremiumBlocked' end,
    overdue_since = coalesce(c.overdue_since, i.due_date),
    grace_period_until = i.due_date + (@gracePeriodDays * interval '1 day'),
    blocked_paid_benefits_at = coalesce(c.blocked_paid_benefits_at, now()),
    blocked_paid_benefits_reason = 'Invoice overdue after grace period',
    updated_at = now()
from habitflow.client_invoices i
where i.client_id = c.id
  and i.status = 'Overdue'
  and i.due_date + (@gracePeriodDays * interval '1 day') < @today
  and coalesce(c.benefits_status, '') not in ('PremiumBlocked','EnterpriseBlocked')", new { today, gracePeriodDays }, ct);

    public Task ReactivateClientAfterApprovedPaymentAsync(Guid clientId, string benefitsStatus, CancellationToken ct = default) => db.ExecuteAsync(@"
update habitflow.clients set payment_status = 'Approved', subscription_status = 'Active', benefits_status = @benefitsStatus, overdue_since = null, grace_period_until = null, blocked_paid_benefits_at = null, blocked_paid_benefits_reason = null, updated_at = now() where id = @clientId", new { clientId, benefitsStatus }, ct);

    public Task<int> ExpireTrialsAsync(CancellationToken ct = default) => db.ExecuteAsync(@"
update habitflow.subscriptions
set status = 'Expired', updated_at = now()
where status in ('Trial','Trialing') and trial_ends_at is not null and trial_ends_at < now()", null, ct);

    public Task<int> SyncUsersWithExpiredTrialsAsync(CancellationToken ct = default) => db.ExecuteAsync(@"
update habitflow.users u
set plan = 'Free', plan_status = 'Inactive', updated_at = now()
from habitflow.subscriptions s
where u.id = s.user_id
  and s.status = 'Expired'
  and u.plan = 'Premium'
  and not exists (select 1 from habitflow.subscriptions a where a.user_id = u.id and a.id <> s.id and a.status in ('Active','Pending','PaymentPending','PastDue','Trial','Trialing'))", null, ct);
}
