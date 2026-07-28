using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record FeatureAccessResult(bool Allowed, string Title, string Message, string? RecommendedPlan = null);
public sealed record FeatureAccessViewModel(string Title, string Explanation, string FeatureCode, string RecommendedPlan, bool LimitReached = false);

/// <summary>Single, friendly boundary for every effective-plan decision.</summary>
public sealed class FeatureAccessService(PlanEntitlementService plans)
{
    public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesForUserAsync(Guid userId, CancellationToken ct = default) =>
        plans.GetFeaturesForUserAsync(userId, ct);
    public async Task<FeatureAccessResult> CheckFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default)
    {
        var allowed = await plans.GetBooleanFeatureAsync(userId, featureCode, ct);
        return allowed
            ? new(true, "Recurso disponível", "Você pode continuar.")
            : new(false, "Este recurso está disponível em outro plano", "Continue usando o HabitFlow gratuitamente ou conheça as opções para acompanhar sua evolução com mais detalhes.", "Ritmo");
    }

    public Task<FeatureAccessResult> RequireFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) =>
        CheckFeatureAsync(userId, featureCode, ct);

    public Task<int?> GetLimitAsync(Guid userId, string featureCode, CancellationToken ct = default) =>
        plans.GetIntegerFeatureAsync(userId, featureCode, ct);

    public async Task<FeatureAccessViewModel> BuildLockedFeatureViewModelAsync(Guid userId, string featureCode, CancellationToken ct = default)
    {
        var result = await CheckFeatureAsync(userId, featureCode, ct);
        return new(result.Title, result.Message, featureCode, result.RecommendedPlan ?? "Ritmo");
    }

    public Task<FeatureAccessViewModel> BuildLimitReachedViewModelAsync(string featureCode, string subject, string recommendedPlan = "Ritmo") =>
        Task.FromResult(new FeatureAccessViewModel($"Seu limite de {subject} foi alcançado", "O que você já criou continua aqui. Você pode pausar um item ou conhecer outras opções.", featureCode, recommendedPlan, true));
}
