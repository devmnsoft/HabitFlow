using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Infrastructure;

namespace HabitFlow.Web.Services;

public sealed class BillingCommunicationJob(IServiceProvider services, IConfiguration configuration, ILogger<BillingCommunicationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("BillingCommunicationJob:Enabled", false)) return;
        var minutes = Math.Max(15, configuration.GetValue("BillingCommunicationJob:IntervalMinutes", 360));
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logs = scope.ServiceProvider.GetRequiredService<IJobExecutionLogRepository>();
        var rules = scope.ServiceProvider.GetRequiredService<IBillingCommunicationRuleRepository>();
        var db = scope.ServiceProvider.GetRequiredService<SqlExecutor>();
        var id = await logs.StartAsync(nameof(BillingCommunicationJob), ct);
        try
        {
            var activeRules = await rules.ListActiveAsync(ct);
            var processed = 0;
            processed += await db.ExecuteAsync(@"insert into habitflow.client_communications(id,client_id,invoice_id,type,channel,title,message,status,sent_at,created_at)
select gen_random_uuid(), i.client_id, i.id, 'BillingDueSoon', 'Internal', 'Fatura vence em breve', 'Sua fatura vence em 3 dias. Se já pagou, desconsidere este aviso.', 'Sent', now(), now()
from habitflow.client_invoices i
where i.status in ('Pending','Open') and i.due_date = current_date + 3
and not exists (select 1 from habitflow.client_communications c where c.client_id=i.client_id and c.invoice_id=i.id and c.type='BillingDueSoon' and c.channel='Internal')", null, ct);
            processed += await db.ExecuteAsync(@"insert into habitflow.client_communications(id,client_id,invoice_id,type,channel,title,message,status,sent_at,created_at)
select gen_random_uuid(), i.client_id, i.id, 'BillingDueToday', 'Internal', 'Fatura vence hoje', 'Sua fatura vence hoje. Mantenha os benefícios pagos ativos realizando o pagamento.', 'Sent', now(), now()
from habitflow.client_invoices i
where i.status in ('Pending','Open') and i.due_date = current_date
and not exists (select 1 from habitflow.client_communications c where c.client_id=i.client_id and c.invoice_id=i.id and c.type='BillingDueToday' and c.channel='Internal')", null, ct);
            processed += await db.ExecuteAsync(@"insert into habitflow.client_communications(id,client_id,invoice_id,type,channel,title,message,status,sent_at,created_at)
select gen_random_uuid(), i.client_id, i.id, case when current_date-i.due_date >= 5 then 'BillingOverdueStrong' else 'BillingOverdue' end, 'Internal', case when current_date-i.due_date >= 5 then 'Regularize sua fatura' else 'Fatura em atraso' end, 'Identificamos fatura vencida. O acesso Free continua disponível e recursos pagos podem ser suspensos após a tolerância.', 'Sent', now(), now()
from habitflow.client_invoices i
where i.status in ('Pending','Open','Overdue') and current_date-i.due_date in (2,5)
and not exists (select 1 from habitflow.client_communications c where c.client_id=i.client_id and c.invoice_id=i.id and c.type in ('BillingOverdue','BillingOverdueStrong') and c.channel='Internal')", null, ct);
            processed += await db.ExecuteAsync(@"insert into habitflow.client_communications(id,client_id,type,channel,title,message,status,sent_at,created_at)
select gen_random_uuid(), c.id, 'BenefitsBlocked', 'Internal', 'Benefícios pagos suspensos', 'Os recursos pagos foram temporariamente suspensos. Sua área Free continua disponível.', 'Sent', now(), now()
from habitflow.clients c
where c.benefits_status in ('PremiumBlocked','EnterpriseBlocked')
and not exists (select 1 from habitflow.client_communications cc where cc.client_id=c.id and cc.type='BenefitsBlocked' and cc.channel='Internal' and cc.created_at::date=current_date)", null, ct);
            await db.ExecuteAsync("insert into habitflow.system_audit_logs(id,severity,source,action,message,created_at) values(gen_random_uuid(),'Info','billing-job','BillingCommunicationJobExecuted',@message,now())", new { message = $"BillingCommunicationJob processou {processed} comunicações e {activeRules.Count} regras ativas." }, ct);
            await logs.FinishAsync(id, "Success", processed, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no BillingCommunicationJob");
            await logs.FinishAsync(id, "Failed", 0, "Falha segura no job de comunicação de cobrança.", ct);
        }
    }
}
