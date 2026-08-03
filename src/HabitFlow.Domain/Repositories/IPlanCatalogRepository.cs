namespace HabitFlow.Domain;

public interface IPlanCatalogRepository
{
    Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default);
    Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default);
    Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default);
    Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default);
}
