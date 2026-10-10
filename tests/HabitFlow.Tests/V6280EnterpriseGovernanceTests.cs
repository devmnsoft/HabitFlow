using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Domain.Security;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6280EnterpriseGovernanceTests
{
    [Fact]
    public void Sso_Disabled_Has_No_Blocking_Issues_And_Local_Login_Remains_Safe()
    {
        var config = new TenantSsoConfiguration(Guid.NewGuid(), false, EnterpriseSsoProvider.None, "", "", "", true, false, null, null);
        var service = new EnterpriseSsoPolicyService();

        Assert.Empty(service.Validate(config, hasEntitlement: false));
        Assert.True(service.CanUseLocalFallback(config, tenantAllowsFallback: false));
    }

    [Fact]
    public void Sso_Enabled_Requires_Enterprise_Entitlement_Https_Callback_And_Signed_Tokens()
    {
        var config = new TenantSsoConfiguration(Guid.NewGuid(), true, EnterpriseSsoProvider.OpenIdConnect,
            "http://idp.example.com", "", "callback//bad", false, true, "tenant", "client_secret=removed");
        var issues = new EnterpriseSsoPolicyService().Validate(config, hasEntitlement: false);

        Assert.Contains(issues, x => x.Code == "sso.entitlement" && x.Severity == "Critical");
        Assert.Contains(issues, x => x.Code == "sso.authority");
        Assert.Contains(issues, x => x.Code == "sso.client_id");
        Assert.Contains(issues, x => x.Code == "sso.callback");
        Assert.Contains(issues, x => x.Code == "sso.signed_tokens");
    }

    [Fact]
    public void WhiteLabel_Blocks_Illegible_Colors_Unsafe_Logo_And_Missing_Entitlement()
    {
        var branding = new TenantBranding(Guid.NewGuid(), "Cliente", "/uploads/../logo.svg", null,
            "#fefefe", "#112233", "http://support.example.com", "Bem-vindo", DateTime.UtcNow);

        var issues = new TenantBrandingPolicyService().Validate(branding, hasEntitlement: false);

        Assert.Contains(issues, x => x.Code == "branding.entitlement");
        Assert.Contains(issues, x => x.Code == "branding.contrast");
        Assert.Contains(issues, x => x.Code == "branding.logo");
        Assert.Contains(issues, x => x.Code == "branding.support_url");
    }

    [Fact]
    public void CustomDomain_Blocks_Duplicate_Invalid_And_Unverified_Activation()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var existing = new[] { new TenantDomain(tenantB, "portal.cliente.com.br", TenantDomainStatus.Active, true, DateTime.UtcNow) };
        var duplicate = new TenantDomain(tenantA, "Portal.Cliente.com.br.", TenantDomainStatus.Active, true, DateTime.UtcNow);
        var invalid = new TenantDomain(tenantA, "localhost", TenantDomainStatus.WaitingDns, false, DateTime.UtcNow);

        var service = new TenantDomainPolicyService();

        Assert.Contains(service.Validate(duplicate, existing, hasEntitlement: true), x => x.Code == "domain.duplicate");
        Assert.Contains(service.Validate(invalid, [], hasEntitlement: true), x => x.Code == "domain.invalid");
        Assert.False(TenantDomainPolicyService.CanActivate(new TenantDomain(tenantA, "novo.cliente.com.br", TenantDomainStatus.WaitingDns, true, DateTime.UtcNow)));
    }

    [Fact]
    public void Permissions_Are_Backend_Enforced_For_Enterprise_Admin_Modules()
    {
        var tenant = Guid.NewGuid();
        var service = new PermissionService();

        Assert.True(service.HasPermission(tenant, tenant, AdminRoles.Owner, AdminPermissions.SsoMfaManage));
        Assert.True(service.HasPermission(tenant, tenant, AdminRoles.Admin, AdminPermissions.WhiteLabelManage));
        Assert.False(service.HasPermission(tenant, Guid.NewGuid(), AdminRoles.Admin, AdminPermissions.DomainsManage));
        Assert.False(service.HasPermission(tenant, tenant, AdminRoles.ReadOnly, AdminPermissions.AiManage));
    }

    [Fact]
    public async Task Enterprise_Entitlements_Fail_Closed_When_Feature_Is_Not_In_Effective_Plan()
    {
        var tenant = Guid.NewGuid();
        var service = new PlanEntitlementService(new FixedEnterprisePlanCatalog(tenant, new Dictionary<string, PlanFeatureValue>
        {
            [PlanFeatureCodes.WhiteLabel] = new(PlanFeatureCodes.WhiteLabel, "White label", "Boolean", true, null, null)
        }));

        Assert.True(await service.CanUseWhiteLabelAsync(tenant));
        Assert.False(await service.CanUseSsoAsync(tenant));
        Assert.False(await service.CanUseCustomDomainAsync(tenant));
    }

    [Fact]
    public void Ai_Governance_Blocks_Disabled_Provider_Model_Outside_AllowList_And_Sensitive_Data()
    {
        var policy = new EnterpriseAiPolicy(Guid.NewGuid(), true, "Groq", "llama-unknown", true, 100, DateTime.UtcNow);
        var issues = new EnterpriseAiPolicyService().Validate(
            policy,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Gemini" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "gemini-1.5-pro" },
            hasEntitlement: false);

        Assert.Contains(issues, x => x.Code == "ai.entitlement");
        Assert.Contains(issues, x => x.Code == "ai.provider");
        Assert.Contains(issues, x => x.Code == "ai.model");
        Assert.Contains(issues, x => x.Code == "ai.sensitive_data");
    }

    [Fact]
    public void Plan_Governance_Exposes_Implemented_Enterprise_Features_Without_Marketing_Fake_ApiKeys()
    {
        var registry = new PlanFeatureImplementationRegistry();

        Assert.Equal(PlanFeatureImplementationStatus.Implemented, registry.Find(PlanFeatureCodes.EnterpriseSso)?.Status);
        Assert.Equal(PlanFeatureImplementationStatus.Implemented, registry.Find(PlanFeatureCodes.WhiteLabel)?.Status);
        Assert.Equal(PlanFeatureImplementationStatus.Planned, registry.Find(PlanFeatureCodes.ApiKeys)?.Status);
    }

    [Fact]
    public void Migration_And_AppSettings_Contain_Enterprise_Safe_Defaults()
    {
        var migration = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrations", "102_v6280_enterprise_governance.sql"));
        var completeScript = File.ReadAllText(RepositoryRootLocator.PathTo("database", "script_completo.sql"));
        var appsettings = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "appsettings.json"));

        Assert.Contains("tenant_sso_configurations", migration);
        Assert.Contains("tenant_branding", migration);
        Assert.Contains("tenant_domains", migration);
        Assert.Contains("enterprise_ai_policies", migration);
        Assert.Contains("ck_tenant_domains_no_activation_without_hash", migration);
        Assert.Contains("-- START include database/migrations/102_v6280_enterprise_governance.sql", completeScript);
        Assert.Contains("\"Sso\"", appsettings);
        Assert.Contains("\"Enabled\": false", appsettings);
        Assert.Contains("\"RequireSignedTokens\": true", appsettings);
    }

    private sealed class FixedEnterprisePlanCatalog(Guid tenantId, IReadOnlyDictionary<string, PlanFeatureValue> features) : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PublicPlan>>([]);

        public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
            Task.FromResult<ClientPlanAccess?>(new(tenantId, PlanCodes.Enterprise, PlanCodes.Enterprise, "Active", null));

        public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<Guid?>(tenantId);

        public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
            Task.FromResult(features);

        public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
    }
}

