using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;
public sealed class OperationsCenterRepository(SqlExecutor db) : IOperationsCenterRepository
{
    public async Task<OperationsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        var counts=await db.QuerySingleOrDefaultAsync<Counts>(@"select
          (select count(*) from habitflow.clients) total_clients,
          (select count(*) from habitflow.clients where status='Active') active_clients,
          (select count(*) from habitflow.clients where status in ('Blocked','Suspended')) blocked_clients,
          (select count(distinct client_id) from habitflow.client_invoices where status in ('Pending','Open','Overdue') and due_date<current_date) overdue_clients,
          (select count(*) from habitflow.users where account_status='Active' and last_activity_at>=now()-interval '30 days') active_users,
          (select count(*) from habitflow.support_tickets where status in ('Open','InProgress')) open_tickets,
          (select count(*) from habitflow.support_tickets where status in ('Open','InProgress') and priority='Critical') critical_tickets,
          (select count(*) from habitflow.structured_log_events where severity in ('Error','Critical') and created_at>=now()-interval '24 hours') recent_errors,
          (select count(*) from habitflow.operational_alerts where status='Active' and type='webhook.failed') failed_webhooks,
          (select count(*) from habitflow.operational_alerts where status='Active' and type='integration.failed') failed_integrations,
          (select count(*) from habitflow.structured_log_events where module='API' and created_at>=now()-interval '24 hours') api_requests24h,
          (select count(*) from habitflow.client_subscriptions where status in ('Pending','Trialing')) pending_subscriptions,
          (select count(*) from habitflow.operational_alerts where status='Active' and type in ('access.denied','security.alert')) security_alerts",null,ct) ?? new();
        var modules=(await db.QueryAsync<ModuleUsage>("select module,count(*) uses from habitflow.structured_log_events where created_at>=now()-interval '30 days' group by module order by uses desc limit 5",null,ct)).ToList();
        var raw=(await db.QueryAsync<TenantRaw>(@"select c.id tenant_id,c.name client_name,c.status::text status,c.plan::text plan,
          0::int enabled_modules,
          (select count(*) from habitflow.users u where u.client_id=c.id and u.account_status='Active' and u.last_activity_at>=now()-interval '30 days') active_users,
          (select count(*) from habitflow.users u where u.client_id=c.id) used_limit,null::int limit,
          (select count(*) from habitflow.support_tickets t where t.client_id=c.id and t.status in ('Open','InProgress')) open_tickets,
          (select count(*) from habitflow.client_invoices i where i.client_id=c.id and i.status in ('Pending','Open','Overdue')) pending_payments,
          (select max(u.last_login_at) from habitflow.users u where u.client_id=c.id) last_access,
          (select count(*) from habitflow.structured_log_events l where l.tenant_id=c.id and l.severity in ('Error','Critical') and l.created_at>=now()-interval '7 days') recent_errors
          from habitflow.clients c order by c.name",null,ct)).ToList();
        var tenants=raw.Select(x=>{var risk=TenantRiskCalculator.Calculate(x.Status,x.RecentErrors,x.PendingPayments,x.OpenTickets,x.UsedLimit,x.Limit);return new TenantRiskRow(x.TenantId,x.ClientName,x.Status,x.Plan,x.EnabledModules,x.ActiveUsers,x.UsedLimit,x.Limit,x.OpenTickets,x.PendingPayments,x.LastAccess,x.RecentErrors,risk.Risk,risk.Reason);}).ToList();
        var alerts=(await db.QueryAsync<OperationalAlertRow>(@"select a.id,a.tenant_id,c.name client_name,a.type,a.severity,a.title,a.occurrences,a.last_occurred_at,a.status from habitflow.operational_alerts a left join habitflow.clients c on c.id=a.tenant_id where a.status='Active' and a.severity='Critical' order by a.last_occurred_at desc limit 20",null,ct)).ToList();
        var status=counts.RecentErrors>0||counts.SecurityAlerts>0?"Atenção":"Operacional";
        return new(counts.TotalClients,counts.ActiveClients,counts.BlockedClients,counts.OverdueClients,counts.ActiveUsers,counts.OpenTickets,counts.CriticalTickets,counts.RecentErrors,counts.FailedWebhooks,counts.FailedIntegrations,counts.ApiRequests24h,counts.PendingSubscriptions,counts.SecurityAlerts,status,modules,tenants,alerts,DateTime.UtcNow);
    }
    public async Task<IReadOnlyList<StructuredLogRow>> SearchLogsAsync(StructuredLogFilter f,CancellationToken ct=default)=>(await db.QueryAsync<StructuredLogRow>(@"select l.id,l.created_at,l.severity,l.event_name,l.module,l.tenant_id,c.name client_name,l.user_id,u.name user_name,l.correlation_id,l.message,l.details::text details from habitflow.structured_log_events l left join habitflow.clients c on c.id=l.tenant_id left join habitflow.users u on u.id=l.user_id where (@From is null or l.created_at>=@From) and (@To is null or l.created_at<@To) and (@TenantId is null or l.tenant_id=@TenantId) and (@UserId is null or l.user_id=@UserId) and (@Module is null or l.module=@Module) and (@Severity is null or l.severity=@Severity) and (@CorrelationId is null or l.correlation_id=@CorrelationId) order by l.created_at desc limit 200 offset @Offset",new{f.From,f.To,f.TenantId,f.UserId,f.Module,f.Severity,f.CorrelationId,Offset=(Math.Max(1,f.Page)-1)*200},ct)).ToList();
    public Task<StructuredLogRow?> GetLogAsync(Guid id,CancellationToken ct=default)=>db.QuerySingleOrDefaultAsync<StructuredLogRow>(@"select l.id,l.created_at,l.severity,l.event_name,l.module,l.tenant_id,c.name client_name,l.user_id,u.name user_name,l.correlation_id,l.message,l.details::text details from habitflow.structured_log_events l left join habitflow.clients c on c.id=l.tenant_id left join habitflow.users u on u.id=l.user_id where l.id=@id",new{id},ct);
    public Task ResolveAlertAsync(Guid id,Guid actorId,CancellationToken ct=default)=>db.ExecuteAsync(@"with resolved as (update habitflow.operational_alerts set status='Resolved',resolved_at=now(),resolved_by=@actorId,updated_at=now() where id=@id and status='Active' returning *) insert into habitflow.operational_alert_history(id,alert_id,tenant_id,user_id,action,occurred_at) select gen_random_uuid(),id,tenant_id,@actorId,'Resolved',now() from resolved",new{id,actorId},ct);
    public async Task<bool> CanConnectAsync(CancellationToken ct=default)=>(await db.QuerySingleOrDefaultAsync<int>("select 1",null,ct))==1;
    public Task<int> PendingMigrationsAsync(CancellationToken ct=default)=>db.QuerySingleOrDefaultAsync<int>("select 0",null,ct);
    private sealed class Counts { public int TotalClients{get;set;} public int ActiveClients{get;set;} public int BlockedClients{get;set;} public int OverdueClients{get;set;} public int ActiveUsers{get;set;} public int OpenTickets{get;set;} public int CriticalTickets{get;set;} public int RecentErrors{get;set;} public int FailedWebhooks{get;set;} public int FailedIntegrations{get;set;} public long ApiRequests24h{get;set;} public int PendingSubscriptions{get;set;} public int SecurityAlerts{get;set;} }
    private sealed class TenantRaw { public Guid TenantId{get;set;} public string ClientName{get;set;}=""; public string Status{get;set;}=""; public string Plan{get;set;}=""; public int EnabledModules{get;set;} public int ActiveUsers{get;set;} public int UsedLimit{get;set;} public int? Limit{get;set;} public int OpenTickets{get;set;} public int PendingPayments{get;set;} public DateTime? LastAccess{get;set;} public int RecentErrors{get;set;} }
}
