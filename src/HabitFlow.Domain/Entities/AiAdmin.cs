namespace HabitFlow.Domain;

public sealed record AiRuntimeSettings(bool Enabled, string DefaultProvider, bool GroqEnabled, bool GeminiEnabled, bool DeepSeekEnabled, string GroqModels, string GeminiModels, string DeepSeekModels, int GlobalDailyLimit, DateTime UpdatedAt);
public sealed record AiPlanLimit(string PlanCode, int DailyLimit);
public sealed record AiUsageEventRow(Guid Id, Guid ClientId, Guid? UserId, string Provider, string Model, string Status, string EventCode, string CorrelationId, int DurationMs, DateTime CreatedAt);
public sealed record AiTenantConsumption(Guid ClientId, int Calls, int Blocked, int Failed);

public interface IAiAdminRepository
{
    Task<AiRuntimeSettings> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(AiRuntimeSettings settings, Guid? userId, CancellationToken ct = default);
    Task<IReadOnlyList<AiPlanLimit>> ListPlanLimitsAsync(CancellationToken ct = default);
    Task SavePlanLimitAsync(string planCode, int dailyLimit, CancellationToken ct = default);
    Task<int> GetEffectiveDailyLimitAsync(Guid clientId, CancellationToken ct = default);
    Task<int> CountUserTodayAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<int> CountGlobalTodayAsync(CancellationToken ct = default);
    Task RecordAsync(Guid clientId, Guid userId, string provider, string model, string status, string eventCode, string correlationId, int durationMs, CancellationToken ct = default);
    Task<IReadOnlyList<AiUsageEventRow>> RecentAsync(Guid? clientId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<AiUsageEventRow>> RecentFailuresAsync(Guid? clientId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<AiTenantConsumption>> ConsumptionAsync(Guid? clientId, CancellationToken ct = default);
}
