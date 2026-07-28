using System.Security.Claims;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V661ContractTests
{
    [Fact]
    public void ClientPlanAccess_preserves_civil_date_and_null()
    {
        var leapDay = new DateOnly(2028, 2, 29);
        Assert.Equal(leapDay, new ClientPlanAccess(Guid.NewGuid(), PlanCodes.Ritmo, PlanCodes.Ritmo, "Active", leapDay).GracePeriodUntil);
        Assert.Null(new ClientPlanAccess(Guid.NewGuid(), PlanCodes.Free, PlanCodes.Free, "Free", null).GracePeriodUntil);
        Assert.Equal(typeof(DateOnly?), typeof(ClientPlanAccess).GetProperty(nameof(ClientPlanAccess.GracePeriodUntil))!.PropertyType);
    }

    [Fact]
    public async Task Navigation_loads_feature_snapshot_once_and_keeps_basic_items()
    {
        var evaluator = new CountingEvaluator();
        var service = new NavigationService(evaluator);
        var user = AuthenticatedUser();

        var items = await service.GetAsync(NavigationContext.Account, user, "/account/plan");

        Assert.Contains(items, x => x.Code == "my-plan");
        Assert.Equal(2, evaluator.FeatureEvaluations);
    }

    [Fact]
    public async Task Navigation_feature_failure_hides_only_dependent_item()
    {
        var service = new NavigationService(new FailingFeatureEvaluator());
        var items = await service.GetAsync(NavigationContext.Personal, AuthenticatedUser(), "/dashboard");
        Assert.Contains(items, x => x.Code == "today");
        Assert.DoesNotContain(items, x => x.Code == "reports");
    }

    private static ClaimsPrincipal AuthenticatedUser() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim("permission", "Client.Users.Manage")], "test"));

    private sealed class CountingEvaluator : INavigationAccessEvaluator
    {
        public int FeatureEvaluations { get; private set; }
        public Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken)
        {
            if (feature is not null) FeatureEvaluations++;
            return Task.FromResult(true);
        }
    }

    private sealed class FailingFeatureEvaluator : INavigationAccessEvaluator
    {
        public Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken) =>
            Task.FromResult(feature is null);
    }
}
