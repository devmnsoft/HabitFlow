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

    
}
