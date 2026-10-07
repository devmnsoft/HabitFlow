using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record ImplantationPage(IReadOnlyList<ImplantationStep> Steps, IReadOnlyList<string> Pendencies, bool CanFinish);

public sealed class ProductActivationService(IProductActivationRepository repository, NotificationService notifications, ILogger<ProductActivationService> logger)
{
    public async Task<ImplantationPage> ChecklistAsync(Guid clientId, CancellationToken ct = default)
    {
        var rows = await repository.ListTenantsAsync(new HomologationQuery(null, null, null, null, null, clientId), ct);
        var row = rows.FirstOrDefault();
        if (row is null) return new([], ["Cliente não encontrado."], false);
        var steps = ImplantationChecklist.Evaluate(Facts(row, await repository.ListOverridesAsync(clientId, ct)));
        var pending = new List<string>();
        if (!row.NotificationsKnown) pending.Add("Preferências de notificação: não disponível");
        if (!row.TemplateUsesKnown) pending.Add("Uso de templates: não disponível");
        if (row.SupportConfigured is null) pending.Add("Contato de suporte: não disponível");
        if (row.AiConfigured is null) pending.Add("Configuração de IA: não disponível");
        return new(steps, pending, ImplantationChecklist.CanFinish(steps));
    }

    public async Task<Result> IgnoreAsync(Guid clientId, Guid userId, string stepCode, string? reason, CancellationToken ct = default)
    {
        var check = ImplantationChecklist.Ignore(stepCode, reason);
        if (check.IsFailure) return check;
        await repository.SaveOverrideAsync(clientId, userId, stepCode, nameof(ImplantationStepStatus.Ignorado), reason!.Trim(), ct);
        return Result.Success();
    }

    public async Task<Result> MarkInProgressAsync(Guid clientId, Guid userId, string stepCode, CancellationToken ct = default)
    {
        if (!ImplantationChecklist.Codes.Contains(stepCode)) return Result.Failure("onboarding.step_invalid", "Etapa de implantação desconhecida.");
        await repository.SaveOverrideAsync(clientId, userId, stepCode, nameof(ImplantationStepStatus.EmAndamento), null, ct);
        return Result.Success();
    }

    public async Task<CustomerSuccessPage> BoardAsync(CancellationToken ct = default)
    {
        var rows = await repository.ListTenantsAsync(new HomologationQuery(null, null, null, null, null, null), ct);
        var known = rows.FirstOrDefault();
        var page = CustomerSuccessBoard.Build(rows, known?.TicketsKnown == true, known?.AiKnown == true, known?.CompletionsKnown == true);
        if (rows.Count == 0) return page;
        try
        {
            foreach (var client in page.Clients.Take(50))
                await repository.RecordCustomerSuccessAsync(client.ClientId, client.Score, client.Health, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Evento de sucesso do cliente não gravado");
            return page with { Pendencies = page.Pendencies.Append("Eventos de sucesso não gravados.").ToList() };
        }
        return page;
    }

    public async Task<HomologationReport> ReportAsync(HomologationQuery query, CancellationToken ct = default)
    {
        var rows = await repository.ListTenantsAsync(query, ct);
        var known = rows.FirstOrDefault();
        var pending = new List<string>();
        if (known is null || !known.AiKnown) pending.Add("Uso de IA: não disponível");
        if (known is null || !known.CompletionsKnown) pending.Add("Conclusões: não disponível");
        if (known is null || !known.TicketsKnown) pending.Add("Chamados: não disponível");
        if (known is null || !known.TemplateUsesKnown) pending.Add("Templates mais usados: não disponível");
        return new HomologationReport(rows, pending);
    }

    public async Task<Result> CreateInAppAlertAsync(Guid clientId, Guid userId, AlertEvaluation alert, CancellationToken ct = default)
    {
        if (!alert.CanCreateInApp)
        {
            await TryNotificationEvent(clientId, userId, alert.Code, "in-app", alert.Condition is null ? "unavailable" : "suppressed", ct);
            return Result.Failure("notification.not_created", alert.Condition is null
                ? "A condição deste aviso não está disponível. Nada foi enviado."
                : "A preferência ou a condição não permite este aviso. Nada foi enviado.");
        }
        var created = await notifications.CreateAsync(userId, alert.Code, alert.Title, alert.ChannelNote, "notification_event", null, ct);
        if (created.IsFailure) return created;
        await TryNotificationEvent(clientId, userId, alert.Code, "in-app", "created", ct);
        return Result.Success();
    }

    private async Task TryNotificationEvent(Guid clientId, Guid? userId, string code, string channel, string status, CancellationToken ct)
    {
        try { await repository.RecordNotificationEventAsync(clientId, userId, code, channel, status, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Evento de notificação não gravado para {ClientId}", clientId); }
    }

    private static ImplantationFacts Facts(TenantActivationRow row, IReadOnlyList<ImplantationOverride> overrides) => new(
        row.CompanyDone, row.PlanReviewed, row.HasAdmin, row.UsersInvited, row.FirstHabit,
        row.TemplateUsesKnown && row.TemplateUses > 0, row.NotificationsKnown && row.NotificationsConfigured,
        true, row.AiConfigured, row.BillingDone, row.SupportConfigured, overrides);
}
