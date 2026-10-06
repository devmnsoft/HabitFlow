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

    public async Task<PlanAccessSnapshot> GetAccessSnapshotAsync(Guid clientId, CancellationToken ct = default)
    {
        var access = await catalog.GetClientAccessAsync(clientId, ct);
        var contracted = access?.ContractedPlanCode ?? PlanCodes.Free;
        var effective = access?.EffectivePlanCode ?? PlanCodes.Free;
        var features = await catalog.GetFeaturesAsync(effective, ct);
        var fullHistory = features.GetValueOrDefault(PlanFeatureCodes.FullHistory)?.BoolValue == true;
        var configuredLimit = features.GetValueOrDefault(PlanFeatureCodes.HistoryDaysLimit)?.IntValue;
        var historyDaysLimit = fullHistory ? -1 : configuredLimit ?? 90;
        return new(contracted, effective, fullHistory, historyDaysLimit);
    }

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
    public async Task<int> GetUsersLimitAsync(Guid clientId, CancellationToken ct = default)
    {
        var planCode = await GetEffectivePlanAsync(clientId, ct);
        var features = await catalog.GetFeaturesAsync(planCode, ct);
        if (!features.TryGetValue(PlanFeatureCodes.UsersLimit, out var feature) || feature.IntValue is null)
            throw new PlanConfigurationException($"O limite de pessoas não está configurado para o plano efetivo '{planCode}'.");

        if (feature.IntValue < -1)
            throw new PlanConfigurationException($"O limite de pessoas configurado para o plano efetivo '{planCode}' é inválido.");

        return feature.IntValue.Value;
    }

    public async Task<bool> CanInviteUserAsync(Guid clientId, int occupiedSlots, CancellationToken ct = default)
    {
        if (occupiedSlots < 0) throw new ArgumentOutOfRangeException(nameof(occupiedSlots));
        return await GetInviteBlockAsync(clientId, occupiedSlots, ct) is null;
    }

    public async Task<PlanInviteBlock?> GetInviteBlockAsync(Guid clientId, int occupiedSlots, CancellationToken ct = default)
    {
        if (occupiedSlots < 0) throw new ArgumentOutOfRangeException(nameof(occupiedSlots));
        var planCode = await GetEffectivePlanAsync(clientId, ct);
        var features = await catalog.GetFeaturesAsync(planCode, ct);
        if (!features.TryGetValue(PlanFeatureCodes.UserInvitations, out var invitations))
            throw new PlanConfigurationException($"O recurso '{PlanFeatureCodes.UserInvitations}' não está configurado para o plano efetivo '{planCode}'.");
        if (invitations.BoolValue != true)
            return new(PlanFeatureCodes.UserInvitations, "Convites não estão incluídos no plano efetivo. Veja o uso do plano ou fale com o comercial.");

        if (!features.TryGetValue(PlanFeatureCodes.UsersLimit, out var limitFeature) || limitFeature.IntValue is null)
            throw new PlanConfigurationException($"O limite de pessoas não está configurado para o plano efetivo '{planCode}'.");
        if (limitFeature.IntValue < -1)
            throw new PlanConfigurationException($"O limite de pessoas configurado para o plano efetivo '{planCode}' é inválido.");
        if (limitFeature.IntValue >= 0 && occupiedSlots >= limitFeature.IntValue)
            return new(PlanFeatureCodes.UsersLimit, "Limite de vagas do plano atingido. Cancele um convite pendente ou fale com o comercial para ampliar a equipe.");
        return null;
    }

    public Task<bool> CanAccessAdvancedReportsAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.AdvancedReports, ct);
    public Task<bool> CanUseFullLibraryAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.FullHabitLibrary, ct);
    public Task<bool> CanExportReportsAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.ReportExportCsv, ct);
    public Task<bool> CanUseSharedRoutinesAsync(Guid userId, CancellationToken ct = default) => GetBooleanFeatureAsync(userId, PlanFeatureCodes.SharedRoutines, ct);
}

public sealed record PlanAccessSnapshot(
    string ContractedPlanCode,
    string EffectivePlanCode,
    bool HasFullHistory,
    int HistoryDaysLimit);

public sealed class PlanConfigurationException(string message) : InvalidOperationException(message);
public sealed record PlanInviteBlock(string Code, string Message);
