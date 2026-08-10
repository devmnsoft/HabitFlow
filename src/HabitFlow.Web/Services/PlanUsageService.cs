using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class PlanUsageService(
    IPlanCatalogRepository catalog,
    IHabitRepository habits,
    IUserGoalRepository goals,
    SubscriptionService subscriptions,
    ILogger<PlanUsageService> logger)
{
    public async Task<PlanUsageViewModel?> BuildAsync(Guid clientId, Guid userId, CancellationToken ct)
    {
        // Resolve the client from the persisted user relationship as well as the authenticated claim.
        // A mismatch is rejected before any usage query, preventing cross-account reads.
        var persistedClientId = await catalog.GetClientIdForUserAsync(userId, ct);
        if (persistedClientId is null || persistedClientId != clientId) return null;

        try
        {
            var access = await catalog.GetClientAccessAsync(clientId, ct);
            var planCode = access?.EffectivePlanCode ?? PlanCodes.Free;
            var features = await catalog.GetFeaturesAsync(planCode, ct);
            var subscription = await subscriptions.GetUserSubscriptionAsync(userId, ct);
            var activeHabits = await habits.CountActiveAsync(clientId, userId, ct);
            var activeGoals = await goals.CountActiveAsync(clientId, userId, ct);
            int? Limit(string code) => features.TryGetValue(code, out var value) ? value.IntValue : null;
            bool Enabled(string code) => features.TryGetValue(code, out var value) && value.BoolValue == true;
            var paid = !planCode.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase)
                && subscription is { Status: SubscriptionStatus.Active or SubscriptionStatus.Trial or SubscriptionStatus.PastDue };
            var publicName = planCode.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase) ? "Gratuito" :
                planCode.Equals(PlanCodes.Ritmo, StringComparison.OrdinalIgnoreCase) ? "Ritmo" : "Evolução";
            var featureUsage = new[]
            {
                new PlanFeatureUsageViewModel("Relatórios", Enabled(PlanFeatureCodes.BasicReports) ? "Resumo do seu progresso disponível." : "Relatórios ampliados não estão incluídos neste plano.", Enabled(PlanFeatureCodes.BasicReports)),
                new PlanFeatureUsageViewModel("Exportações", Enabled(PlanFeatureCodes.ReportExportCsv) ? "Exportação CSV disponível." : "Exportações não estão incluídas neste plano.", Enabled(PlanFeatureCodes.ReportExportCsv)),
                new PlanFeatureUsageViewModel("Biblioteca de hábitos", Enabled(PlanFeatureCodes.FullHabitLibrary) ? "Biblioteca completa disponível." : "Uma seleção da biblioteca está disponível.", Enabled(PlanFeatureCodes.FullHabitLibrary))
            };
            var historyDays = Limit(PlanFeatureCodes.HistoryDaysLimit);
            return new(publicName, paid,
                new("Hábitos ativos", activeHabits, Limit(PlanFeatureCodes.ActiveHabitsLimit)),
                new("Objetivos ativos", activeGoals, Limit(PlanFeatureCodes.ActiveGoalsLimit)),
                featureUsage, featureUsage.Where(x => !x.Available).ToArray(),
                Enabled(PlanFeatureCodes.FullHistory) ? "Seu histórico completo está disponível." : historyDays is > 0 ? $"Histórico dos últimos {historyDays} dias." : "Este recurso ainda não tem uso registrado.",
                subscription?.Status.ToString(), subscription?.CurrentPeriodEnd,
                paid ? null : new("Mais espaço para sua rotina", "Compare apenas os recursos que já estão disponíveis e escolha no seu ritmo.", "/plans"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao montar uso do plano para usuário {UserId}", userId);
            throw;
        }
    }
}
