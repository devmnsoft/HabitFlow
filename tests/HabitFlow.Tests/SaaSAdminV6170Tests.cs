using HabitFlow.Application;
using HabitFlow.Domain.Security;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SaaSAdminV6170Tests
{
    [Fact]
    public void Tenant_admin_cannot_cross_tenant_boundary()
    {
        var service = new PermissionService();
        var own = Guid.NewGuid();
        Assert.True(service.HasPermission(own, own, AdminRoles.Admin, AdminPermissions.UsersInvite));
        Assert.False(service.HasPermission(own, Guid.NewGuid(), AdminRoles.Admin, AdminPermissions.UsersInvite));
    }

    [Fact]
    public void Member_has_no_administrative_permission()
    {
        Assert.False(new PermissionService().HasPermission(Guid.NewGuid(), Guid.NewGuid(), AdminRoles.Member, AdminPermissions.DashboardRead));
    }

    [Fact]
    public void Critical_permissions_are_canonical_and_not_accidentally_granted()
    {
        var tenant = Guid.NewGuid();
        var service = new PermissionService();
        Assert.True(service.HasPermission(tenant, tenant, AdminRoles.Owner, AdminPermissions.PrivacyManage));
        Assert.False(service.HasPermission(tenant, tenant, AdminRoles.ReadOnly, AdminPermissions.FeatureFlagsManage));
        Assert.False(service.HasPermission(tenant, tenant, AdminRoles.Owner, "unknown.permission"));
    }

    [Fact]
    public void Migration_defines_secure_invites_audit_privacy_and_scoped_flags()
    {
        var root = RepositoryRootLocator.Find();
        var sql = File.ReadAllText(Path.Combine(root, "database/migrations/074_v6170_saas_admin_lgpd.sql"));
        Assert.Contains("token_hash", sql); Assert.Contains("expires_at", sql); Assert.Contains("accepted_at", sql); Assert.Contains("revoked_at", sql);
        Assert.Contains("client_id uuid not null", sql); Assert.Contains("correlation_id", sql); Assert.Contains("legal_hold_reason", sql);
        Assert.Contains("environment", sql); Assert.Contains("starts_at", sql); Assert.Contains("ends_at", sql);
        Assert.DoesNotContain(" token varchar", sql.ToLowerInvariant());
    }

    [Fact]
    public void Complete_script_and_schema_validator_include_release()
    {
        var root = RepositoryRootLocator.Find();
        Assert.Contains("074_v6170_saas_admin_lgpd.sql", File.ReadAllText(Path.Combine(root, "database/script_completo.sql")));
        Assert.Contains("v6.17.0 SaaS administration contracts", File.ReadAllText(Path.Combine(root, "database/validate_schema_habitflow.sql")));
    }
}
