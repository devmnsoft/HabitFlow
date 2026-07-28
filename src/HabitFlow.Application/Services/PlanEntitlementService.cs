using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class PlanEntitlementService(IPlanCatalogRepository catalog)
{
    public async Task<string> GetContractedPlanAsync(Guid clientId, CancellationToken ct = default) =>
        (await catalog.GetClientAccessAsync(clientId, ct))?.ContractedPlanCode ?? PlanCodes.Free;

    public async Task<string> GetEffectivePlanAsync(Guid clientId, CancellationToken ct = default) =>
        (await catalog.GetClientAccessAsync(clientId, ct))?.EffectivePlanCode ?? PlanCodes.Free;

    public async Task<string> GetEffectivePlanForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var clientId = await catalog.GetClientIdForUserAsync(userId, ct);
        return clientId is null ? PlanCodes.Free : await GetEffectivePlanAsync(clientId.Value, ct);
    }

    public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetPlanFeaturesAsync(string planCode, CancellationToken ct = default) => catalog.GetFeaturesAsync(planCode, ct);

    public async Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var planCode = await GetEffectivePlanForUserAsync(userId, ct);
        return await GetPlanFeaturesAsync(planCode, ct);
    }

    public async Task<PlanFeatureValue?> GetFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default)
    {
        var features = await GetPlanFeaturesAsync(await GetEffectivePlanForUserAsync(userId, ct), ct);
        return features.GetValueOrDefault(featureCode);
    }

    public async Task<bool> GetBooleanFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) => (await GetFeatureAsync(userId, featureCode, ct))?.BoolValue == true;
    public async Task<int?> GetIntegerFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) => (await GetFeatureAsync(userId, featureCode, ct))?.IntValue;
    public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, featureCode, ct);
    public async Task<bool> CanCreateHabitAsync(Guid userId, int activeHabits, CancellationToken ct = default) { var limit = await GetIntegerFeatureAsync(userId, PlanFeatureCodes.ActiveHabitsLimit, ct); return limit is < 0 or null || activeHabits < limit; }
    public async Task<bool> CanInviteUserAsync(Guid clientId, int activeUsers, CancellationToken ct = default) { var features = await catalog.GetFeaturesAsync(await GetEffectivePlanAsync(clientId, ct), ct); var limit = features.GetValueOrDefault(PlanFeatureCodes.UsersLimit)?.IntValue; return limit is < 0 or null || activeUsers < limit; }
    public Task<bool> CanAccessAdvancedReportsAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.AdvancedReports, ct);
    public Task<bool> CanUseFullLibraryAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.FullHabitLibrary, ct);
    public Task<bool> CanExportReportsAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.ReportExportCsv, ct);
    public Task<bool> CanUseSharedRoutinesAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.SharedRoutines, ct);
}
