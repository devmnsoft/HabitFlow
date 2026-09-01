using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SaaSGovernanceV6192Tests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static TenantAccessContext Context(UserRole role, Guid? tenant = null, TenantStatus status = TenantStatus.Active, params string[] modules) =>
        new(Guid.NewGuid(), tenant, role, status, modules.ToHashSet(StringComparer.OrdinalIgnoreCase));

    [Fact] public void SuperAdminAccessesEveryTenant() => Assert.True(TenantAccessPolicy.CanAccessTenant(Context(UserRole.SuperAdmin), TenantB));
    [Fact] public void CommonUserCannotAccessAnotherTenant() => Assert.False(TenantAccessPolicy.CanAccessTenant(Context(UserRole.User, TenantA), TenantB));
    [Fact] public void TenantAdminOnlyManagesOwnTenant()
    {
        var actor = Context(UserRole.TenantAdmin, TenantA);
        Assert.True(TenantAccessPolicy.CanAccessTenant(actor, TenantA));
        Assert.False(TenantAccessPolicy.CanAccessTenant(actor, TenantB));
        Assert.True(TenantAccessPolicy.CanManageUsers(actor.Role));
    }
    [Fact] public void DisabledModuleIsForbidden() => Assert.False(TenantAccessPolicy.CanUseModule(Context(UserRole.User, TenantA, TenantStatus.Active, TenantModules.Habits), TenantModules.Analytics));
    [Fact] public void CommercialBlockDeniesAndUnlockRestoresAccess()
    {
        Assert.False(TenantAccessPolicy.CanUseModule(Context(UserRole.User, TenantA, TenantStatus.CommerciallyBlocked, TenantModules.Habits), TenantModules.Habits));
        Assert.True(TenantAccessPolicy.CanUseModule(Context(UserRole.User, TenantA, TenantStatus.Active, TenantModules.Habits), TenantModules.Habits));
    }
    [Fact] public void RoleCannotBeEscalated() => Assert.False(TenantAccessPolicy.CanGrant(UserRole.TenantAdmin, UserRole.TenantOwner));
    [Fact] public void AmbiguousDocumentLoginRequiresTenantChoice()
    {
        Assert.True(TenantLoginSelection.RequiresSelection(2));
        Assert.False(TenantLoginSelection.CanComplete(2, null));
        Assert.True(TenantLoginSelection.CanComplete(2, TenantA));
    }
    [Fact] public void ManualChargeRequiresReason()
    {
        Assert.False(TenantAccessPolicy.IsValidManualCharge(new(TenantA, 10, DateOnly.FromDateTime(DateTime.UtcNow), "Mensalidade", " ")));
        Assert.True(TenantAccessPolicy.IsValidManualCharge(new(TenantA, 10, DateOnly.FromDateTime(DateTime.UtcNow), "Mensalidade", "Ajuste comercial autorizado")));
    }
    [Theory]
    [InlineData("529.982.247-25", true)] [InlineData("111.111.111-11", false)]
    public void CpfValidation(string value, bool expected) => Assert.Equal(expected, new DocumentValidator().ValidateCpf(value));
    [Theory]
    [InlineData("18.160.057/0001-13", true)] [InlineData("11.111.111/1111-11", false)]
    public void CnpjValidation(string value, bool expected) => Assert.Equal(expected, new DocumentValidator().ValidateCnpj(value));
}
