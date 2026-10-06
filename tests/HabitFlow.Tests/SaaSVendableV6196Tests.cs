using Xunit;

namespace HabitFlow.Tests;

/// <summary>
/// v6.19.6 — SaaS vendável e homologável: catálogo Team/Enterprise, estados comerciais completos,
/// escopo multi-tenant, claims de tenant, eventos de billing e limites por plano aplicados no backend.
/// As asserções validam o que foi entregue nesta versão (fontes, schema e SQL completo).
/// </summary>
public sealed class SaaSVendableV6196Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root, Path.Combine(parts)));

    [Fact]
    public void team_enterprise_migration_adds_catalog_states_and_tenant_tables_idempotently()
    {
        var path = Path.Combine(Root, "database", "migrations", "089_v6196_saas_team_enterprise_catalog.sql");
        Assert.True(File.Exists(path), "migração 089 ausente");
        var sql = Read("database", "migrations", "089_v6196_saas_team_enterprise_catalog.sql");

        // Estados de assinatura ampliados (mantendo os antigos).
        Assert.Contains("'PaymentPending','Active','Trial','Trialing','PastDue'", sql);
        Assert.Contains("'ManualReview','Suspended','Failed','Inactive'", sql);

        // Estados comerciais do cliente.
        Assert.Contains("ck_habitflow_clients_subscription_status", sql);
        Assert.Contains("ix_clients_subscription_status", sql);

        // Novas features e planos com códigos estáveis; enterprise é venda por contato.
        Assert.Contains("'teams','Gestão de times'", sql);
        Assert.Contains("corporate_features", sql);
        Assert.Contains(",'team','Team'", sql);
        Assert.Contains(",'enterprise','Enterprise'", sql);
        Assert.Contains("'Available',false,40", sql);
        Assert.Contains("'Contact',false,50", sql);

        // Preços reais do Team (anual com economia).
        Assert.Contains("'Monthly',79.90", sql);
        Assert.Contains("'Yearly',799.00", sql);

        // Limite de usuários por plano (team=10, enterprise=-1).
        Assert.Contains("WHEN 'users_limit' THEN CASE WHEN p.code='team' THEN 10 ELSE -1 END", sql);

        // Tenant MNSOFT vira Enterprise.
        Assert.Contains("contracted_plan_code='enterprise'", sql);

        // Tabelas tenant_* idempotentes.
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.tenant_feature_flags", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.tenant_commercial_status", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }

    [Fact]
    public void script_completo_contains_new_tenant_tables()
    {
        var sql = Read("database", "script_completo.sql");
        Assert.Contains("habitflow.tenant_feature_flags", sql);
        Assert.Contains("habitflow.tenant_commercial_status", sql);
        // Sem diretivas de inclusão: arquivo autossuficiente.
        Assert.DoesNotContain("\\i ", sql);
        Assert.DoesNotContain("\\ir ", sql);
    }

    [Fact]
    public void plan_and_subscription_enums_support_vendable_states()
    {
        var plans = Read("src", "HabitFlow.Domain", "Entities", "PlanCatalog.cs");
        Assert.Contains("public const string Team = \"team\";", plans);
        Assert.Contains("public const string Enterprise = \"enterprise\";", plans);
        Assert.Contains("public const string Teams = \"teams\";", plans);

        var sub = Read("src", "HabitFlow.Domain", "Enums", "BillingEnums.cs");
        Assert.Contains("enum SubscriptionStatus", sub);
        Assert.Contains("Free, Suspended", sub);

        var client = Read("src", "HabitFlow.Domain", "Enums", "ClientEnums.cs");
        Assert.Contains("enum ClientSubscriptionStatus", client);
        Assert.Contains("PaymentPending", client);
        Assert.Contains("ManualReview", client);
    }

    [Fact]
    public void client_service_enforces_multi_tenant_scope()
    {
        var svc = Read("src", "HabitFlow.Application", "Services", "ClientService.cs");
        Assert.Contains("CurrentTenantService tenant", svc);
        Assert.Contains("tenant.denied", svc);
        Assert.Contains("IsSuperAdmin()", svc);
        Assert.Contains("CanAccessClient(", svc);
        Assert.Contains("security.access_denied", svc);

        var repo = Read("src", "HabitFlow.Domain", "Repositories", "IClientRepository.cs");
        Assert.Contains("Guid? clientId = null", repo);
        Assert.Contains("GetEnabledModulesAsync", repo);
        Assert.Contains("RecordCommercialStatusChangeAsync", repo);
    }

    [Fact]
    public void session_cookie_emits_tenant_status_and_module_claims()
    {
        var auth = Read("src", "HabitFlow.Web", "Configuration", "AuthenticationConfig.cs");
        Assert.Contains("\"tenant_status\"", auth);
        Assert.Contains("\"tenant_module\"", auth);
        Assert.Contains("GetEnabledModulesAsync", auth);
        Assert.Contains("TenantStatus.CommerciallyBlocked", auth);
    }

    [Fact]
    public void super_admin_can_access_global_admin_dashboard()
    {
        var svc = Read("src", "HabitFlow.Application", "Services", "AdminOperationalServices.cs");
        Assert.Contains("UserRole.Admin or UserRole.SuperAdmin", svc);
    }

    [Fact]
    public void entitlement_maps_team_enterprise_and_enforces_invite_limit()
    {
        var ent = Read("src", "HabitFlow.Application", "Services", "EntitlementService.cs");
        Assert.Contains("PlanCodes.Team => ClientPlan.Premium", ent);
        Assert.Contains("PlanCodes.Enterprise => ClientPlan.Enterprise", ent);
        Assert.Contains("clients.SearchAsync(null, null, null, 0, 500, null, ct)", ent);

        var invite = Read("src", "HabitFlow.Application", "Services", "UserInviteService.cs");
        Assert.Contains("CanInviteUserAsync", invite);
        Assert.Contains("billing.entitlement.blocked", invite);
        Assert.Contains("PlanLimitException", invite);
        Assert.Contains("IBillingEventLogRepository", invite);
    }

    [Fact]
    public void plans_ui_exposes_team_enterprise_and_contact_checkout()
    {
        var comparison = Read("src", "HabitFlow.Web", "Views", "Plans", "Partials", "_PlanComparisonTable.cshtml");
        Assert.Contains("<th>Team</th>", comparison);
        Assert.Contains("<th>Enterprise</th>", comparison);
        Assert.Contains("Premium mensal", comparison);
        Assert.Contains("Premium anual", comparison);

        var card = Read("src", "HabitFlow.Web", "Views", "Plans", "Partials", "_CommercialPlanCard.cshtml");
        Assert.Contains("/billing/checkout", card);
        Assert.Contains("Entrar para assinar", card);
        Assert.Contains("Falar com a MNSOFT", card);

        var billing = Read("src", "HabitFlow.Web", "Controllers", "BillingController.cs");
        Assert.Contains("PlanCodes.Enterprise", billing);

        var models = Read("src", "HabitFlow.Web", "Models", "PlanLandingPageViewModels.cs");
        Assert.Contains("string Enterprise = \"Sob consulta\"", models);
    }

    [Fact]
    public void billing_event_log_repository_is_registered_and_structured()
    {
        var ifacePath = Path.Combine(Root, "src", "HabitFlow.Domain", "Repositories", "IBillingEventLogRepository.cs");
        Assert.True(File.Exists(ifacePath));
        Assert.Contains("LogAsync", File.ReadAllText(ifacePath));

        var di = Read("src", "HabitFlow.Infrastructure", "DependencyInjection.cs");
        Assert.Contains("IBillingEventLogRepository, BillingEventLogRepository", di);

        var events = Read("src", "HabitFlow.Application", "Observability", "ApplicationEvents.cs");
        Assert.Contains("billing.entitlement.blocked", events);
    }

    [Fact]
    public void domain_owns_shared_kernel_types()
    {
        var shared = Path.Combine(Root, "src", "HabitFlow.Domain", "SharedKernel.cs");
        Assert.True(File.Exists(shared), "SharedKernel.cs ausente no Domain");
        Assert.Contains("namespace HabitFlow.Shared;", File.ReadAllText(shared));
    }
}
