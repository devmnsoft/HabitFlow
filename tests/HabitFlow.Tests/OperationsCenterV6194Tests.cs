using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;
public sealed class OperationsCenterV6194Tests
{
    [Theory]
    [InlineData("Active",0,0,0,2,10,"Baixo")]
    [InlineData("Active",3,1,0,2,10,"Alto")]
    [InlineData("Blocked",0,0,0,0,null,"Crítico")]
    public void Tenant_risk_is_calculated_from_real_signals(string status,int errors,int payments,int tickets,int used,int? limit,string expected)
        => Assert.Equal(expected,TenantRiskCalculator.Calculate(status,errors,payments,tickets,used,limit).Risk);

    [Fact] public void Logs_mask_secrets_and_credentials()
    {
        var sanitizer=new LogSanitizer(); var value=sanitizer.Sanitize("cpf=123.456.789-09 token=abc password=hunter2");
        Assert.DoesNotContain("abc",value); Assert.DoesNotContain("hunter2",value);
    }

    [Fact] public void Operations_routes_require_superadmin()
    {
        var source=File.ReadAllText(Path.Combine(Root(),"src/HabitFlow.Web/Controllers/AdminOperationsController.cs"));
        Assert.Contains("[Authorize(Policy=\"RequireSuperAdmin\")]",source); Assert.Contains("[HttpGet(\"operations\")]",source); Assert.Contains("[HttpGet(\"logs\")]",source);
    }

    [Fact] public void Migration_groups_active_alerts_and_indexes_tenant_data()
    {
        var sql=File.ReadAllText(Path.Combine(Root(),"database/migrations/085_v6194_operations_center.sql"));
        Assert.Contains("unique index if not exists ux_operational_alert_active_dedup",sql); Assert.Contains("tenant_id",sql); Assert.Contains("operational_alert_history",sql);
    }
    private static string Root()=>Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
}
