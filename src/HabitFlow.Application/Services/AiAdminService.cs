using System.Text.RegularExpressions;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class AiAdminService(IAiAdminRepository repository, AiUsageLimiter memory, ILogger<AiAdminService> logger)
{
    private static readonly Regex ModelList = new(@"^([A-Za-z0-9._:-]+)(,[A-Za-z0-9._:-]+)*$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public Task<AiRuntimeSettings> SettingsAsync(CancellationToken ct) => repository.GetSettingsAsync(ct);
    public Task<IReadOnlyList<AiPlanLimit>> PlanLimitsAsync(CancellationToken ct) => repository.ListPlanLimitsAsync(ct);
    public Task<IReadOnlyList<AiUsageEventRow>> RecentAsync(Guid? clientId, CancellationToken ct) => repository.RecentAsync(clientId, 30, ct);
    public Task<IReadOnlyList<AiUsageEventRow>> FailuresAsync(Guid? clientId, CancellationToken ct) => repository.RecentFailuresAsync(clientId, 20, ct);
    public Task<IReadOnlyList<AiTenantConsumption>> ConsumptionAsync(Guid? clientId, CancellationToken ct) => repository.ConsumptionAsync(clientId, ct);
    public Task<int> EffectiveLimitAsync(Guid clientId, CancellationToken ct) => repository.GetEffectiveDailyLimitAsync(clientId, ct);

    public async Task<bool> AllowAsync(Guid clientId, Guid userId, UserPlan plan, CancellationToken ct)
    {
        try
        {
            var settings = await repository.GetSettingsAsync(ct);
            var limit = await repository.GetEffectiveDailyLimitAsync(clientId, ct);
            var used = await repository.CountUserTodayAsync(clientId, userId, ct);
            var global = await repository.CountGlobalTodayAsync(ct);
            return used < limit && global < settings.GlobalDailyLimit;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("ai.limit.fallback ErrorType={ErrorType}", ex.GetType().Name);
            return memory.TryConsume(clientId, userId, plan);
        }
    }

    public async Task<string?> SaveAsync(AiRuntimeSettings settings, IReadOnlyList<AiPlanLimit> limits, Guid? userId, CancellationToken ct)
    {
        if (settings.DefaultProvider is not ("" or "Groq" or "Gemini" or "DeepSeek")) return "O formato informado não é válido.";
        if (settings.GlobalDailyLimit is < 0 or > 100000) return "O formato informado não é válido.";
        foreach (var limit in limits)
        {
            if (limit.PlanCode is not ("free" or "ritmo" or "team" or "enterprise")) return "O formato informado não é válido.";
            if (limit.DailyLimit is < 0 or > 100000) return "O formato informado não é válido.";
        }
        foreach (var models in new[] { settings.GroqModels, settings.GeminiModels, settings.DeepSeekModels })
            if (!string.IsNullOrWhiteSpace(models) && !ModelList.IsMatch(models.Trim())) return "O formato informado não é válido.";
        await repository.SaveSettingsAsync(settings, userId, ct);
        foreach (var limit in limits) await repository.SavePlanLimitAsync(limit.PlanCode, limit.DailyLimit, ct);
        return null;
    }

    public async Task RecordAsync(Guid clientId, Guid userId, string provider, string model, string status, string eventCode, string correlationId, long durationMs, CancellationToken ct)
    {
        logger.LogInformation(EventFor(eventCode), "{Code} CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} Provider={Provider} Model={Model} Status={Status} DurationMs={DurationMs}", eventCode, correlationId, clientId, userId, provider, model, status, durationMs);
        try
        {
            await repository.RecordAsync(clientId, userId, provider, Safe(model), Safe(status), eventCode, Safe(correlationId), (int)Math.Clamp(durationMs, 0, int.MaxValue), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("ai.audit.persist_failed ErrorType={ErrorType}", ex.GetType().Name);
        }
    }

    public bool ProviderEnabled(AiRuntimeSettings settings, string provider) => provider.ToLowerInvariant() switch
    {
        "groq" => settings.GroqEnabled,
        "gemini" => settings.GeminiEnabled,
        "deepseek" => settings.DeepSeekEnabled,
        _ => false
    };

    public bool ModelListed(AiRuntimeSettings settings, string provider, string model)
    {
        var raw = provider.ToLowerInvariant() switch
        {
            "groq" => settings.GroqModels,
            "gemini" => settings.GeminiModels,
            "deepseek" => settings.DeepSeekModels,
            _ => ""
        };
        var list = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return list.Length == 0 || list.Any(item => item.Equals(model, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasKey(string provider) => !string.IsNullOrWhiteSpace(AiSecretResolver.ForProvider(provider));
    private static string Safe(string? value) => (value ?? "").Length <= 100 ? value ?? "" : value![..100];

    private static EventId EventFor(string code) => code switch
    {
        "ai.chat.opened" => ApplicationEvents.AiChatOpened,
        "ai.request.started" => ApplicationEvents.AiRequestStarted,
        "ai.request.completed" => ApplicationEvents.AiRequestCompleted,
        "ai.request.failed" => ApplicationEvents.AiRequestFailed,
        "ai.request.blocked_by_plan" => ApplicationEvents.AiRequestBlockedByPlan,
        "ai.request.blocked_by_guardrail" => ApplicationEvents.AiRequestBlockedByGuardrail,
        "ai.provider.unavailable" => ApplicationEvents.AiProviderUnavailable,
        "ai.provider.timeout" => ApplicationEvents.AiProviderTimeout,
        "ai.settings.updated" => ApplicationEvents.AiSettingsUpdated,
        "design.validation.failed" => ApplicationEvents.DesignValidationFailed,
        "ui.form.validation.failed" => ApplicationEvents.UiFormValidationFailed,
        "ai.security.prompt_injection_detected" => ApplicationEvents.AiSecurityPromptInjectionDetected,
        "ai.security.data_exfiltration_blocked" => ApplicationEvents.AiSecurityDataExfiltrationBlocked,
        "ai.security.cross_tenant_context_blocked" => ApplicationEvents.AiSecurityCrossTenantContextBlocked,
        "ai.security.secret_request_blocked" => ApplicationEvents.AiSecuritySecretRequestBlocked,
        "security.login.failed" => ApplicationEvents.SecurityLoginFailed,
        "security.login.succeeded" => ApplicationEvents.SecurityLoginSucceeded,
        "security.access_denied" => ApplicationEvents.SecurityAccessDenied,
        "security.rate_limit_triggered" => ApplicationEvents.SecurityRateLimitTriggered,
        "security.suspicious_activity_detected" => ApplicationEvents.SecuritySuspiciousActivityDetected,
        "security.tenant_isolation_violation_blocked" => ApplicationEvents.SecurityTenantIsolationViolationBlocked,
        "security.secret_exposure_prevented" => ApplicationEvents.SecuritySecretExposurePrevented,
        "security.admin_action_audited" => ApplicationEvents.SecurityAdminActionAudited,
        _ => new EventId(0, code)
    };
}
