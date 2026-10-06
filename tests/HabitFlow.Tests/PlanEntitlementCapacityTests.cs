using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PlanEntitlementCapacityTests
{
    [Theory]
    [InlineData(10, 9, true)]
    [InlineData(10, 10, false)]
    [InlineData(-1, 100, true)]
    public async Task Admission_uses_explicit_effective_plan_limit(int? limit, int occupiedSlots, bool expected)
    {
        var service = new PlanEntitlementService(new FixedPlanCatalog(limit));

        Assert.Equal(expected, await service.CanInviteUserAsync(Guid.NewGuid(), occupiedSlots));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-2)]
    public async Task Missing_or_invalid_limit_fails_closed(int? limit)
    {
        var service = new PlanEntitlementService(new FixedPlanCatalog(limit));

        await Assert.ThrowsAsync<PlanConfigurationException>(() => service.GetUsersLimitAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Invitations_are_blocked_when_the_effective_plan_does_not_include_them()
    {
        var service = new PlanEntitlementService(new FixedPlanCatalog(10, invitationsEnabled: false));

        Assert.False(await service.CanInviteUserAsync(Guid.NewGuid(), 0));
    }

    private sealed class FixedPlanCatalog(int? userLimit, bool invitationsEnabled = true) : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PublicPlan>>(Array.Empty<PublicPlan>());

        public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
            Task.FromResult<ClientPlanAccess?>(new(clientId, PlanCodes.Team, PlanCodes.Team, "Active", null));

        public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<Guid?>(null);

        public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default)
        {
            var features = new Dictionary<string, PlanFeatureValue>
            {
                [PlanFeatureCodes.UserInvitations] = new(PlanFeatureCodes.UserInvitations, "Convites de pessoas", "Boolean", invitationsEnabled, null, null)
            };
            if (userLimit is not null)
                features[PlanFeatureCodes.UsersLimit] = new(PlanFeatureCodes.UsersLimit, "Pessoas da conta", "Integer", null, userLimit, null);
            return Task.FromResult<IReadOnlyDictionary<string, PlanFeatureValue>>(features);
        }

        public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>(Array.Empty<PlanIntegrityCatalogItem>());
    }
}
