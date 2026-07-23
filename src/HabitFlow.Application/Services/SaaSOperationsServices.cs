using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class ClientOnboardingService(IClientOnboardingRepository repository)
{
    public Task<ClientOnboarding> GetOrCreateAsync(Guid clientId, CancellationToken ct = default) => repository.GetOrCreateAsync(clientId, ct);
    public Task CompleteCompanyAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "company_data_completed", true, ct);
    public Task CompleteUsersAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "first_user_invited", true, ct);
    public Task CompleteBillingAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "billing_data_completed", true, ct);
    public Task CompletePlanAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "plan_reviewed", true, ct);
    public Task CompleteFirstHabitAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "first_habit_created", true, ct);
    public Task FinishAsync(Guid clientId, CancellationToken ct = default) => repository.UpdateStepAsync(clientId, "completed", true, ct);
    public static IReadOnlyList<OnboardingChecklistItem> BuildChecklist(ClientOnboarding o) => new[]
    {
        new OnboardingChecklistItem("Dados da conta confirmados", o.CompanyDataCompleted, "🏢", "/admin/company", "Complete os dados da empresa para manter sua conta organizada."),
        new OnboardingChecklistItem("Dados de cobrança preenchidos", o.BillingDataCompleted, "💳", "/admin/company#billing", "Informe dados de cobrança sem armazenar dados sensíveis de pagamento."),
        new OnboardingChecklistItem("Primeiro usuário convidado", o.FirstUserInvited, "✉️", "/admin/users/invite", "Convide sua equipe com isolamento por cliente."),
        new OnboardingChecklistItem("Primeiro hábito criado", o.FirstHabitCreated, "✅", "/habits", "Comece com um hábito próprio ou modelo da biblioteca."),
        new OnboardingChecklistItem("Plano revisado", o.PlanReviewed, "⭐", "/plans", "Revise o plano atual e benefícios disponíveis."),
        new OnboardingChecklistItem("Suporte conhecido", true, "🛟", "/admin/support", "Conheça a central de suporte e canais oficiais."),
        new OnboardingChecklistItem("Onboarding concluído", o.Completed, "🚀", "/admin/onboarding", "Finalize a implantação quando tudo estiver pronto.")
    };
}

public sealed class ClientCommunicationService(IClientCommunicationRepository communications, NotificationService notifications, ILogger<ClientCommunicationService> logger)
{
    public async Task<Result> CreateInternalMessageAsync(Guid clientId, Guid? userId, string type, string title, string message, Guid? invoiceId = null, CancellationToken ct = default)
    {
        if (invoiceId.HasValue && await communications.ExistsAsync(clientId, invoiceId, type, "Internal", ct)) return Result.Success();
        var item = new ClientCommunication(Guid.NewGuid(), clientId, userId, invoiceId, type, "Internal", title, message, "Sent", DateTime.UtcNow, null, DateTime.UtcNow);
        await communications.CreateAsync(item, ct);
        if (userId.HasValue) await notifications.CreateAsync(userId.Value, type, title, message, "client_communication", item.Id, ct);
        logger.LogInformation("Comunicação interna {Type} criada para cliente {ClientId}", type, clientId);
        return Result.Success();
    }
    public Task<IReadOnlyList<ClientCommunication>> ListByClientAsync(Guid clientId, ClientCommunicationFilter filter, CancellationToken ct = default) => communications.ListByClientAsync(clientId, filter with { ClientId = clientId }, ct);
    public Task<IReadOnlyList<ClientCommunication>> ListAllAsync(ClientCommunicationFilter filter, CancellationToken ct = default) => communications.ListAllAsync(filter, ct);
    public Task MarkAsReadAsync(Guid clientId, Guid id, CancellationToken ct = default) => communications.MarkAsReadAsync(clientId, id, DateTime.UtcNow, ct);
    public Task<Result> CreateBillingNoticeAsync(Guid clientId, Guid? userId, Guid invoiceId, string title, string message, CancellationToken ct = default) => CreateInternalMessageAsync(clientId, userId, "Billing", title, message, invoiceId, ct);
    public Task<Result> CreatePaymentApprovedNoticeAsync(Guid clientId, Guid? userId, Guid? invoiceId, CancellationToken ct = default) => CreateInternalMessageAsync(clientId, userId, "PaymentApproved", "Pagamento confirmado", "Seus benefícios pagos estão ativos novamente.", invoiceId, ct);
    public Task<Result> CreateBenefitsBlockedNoticeAsync(Guid clientId, Guid? userId, Guid? invoiceId, CancellationToken ct = default) => CreateInternalMessageAsync(clientId, userId, "BenefitsBlocked", "Benefícios Premium suspensos", "Os recursos pagos foram temporariamente suspensos. A área gratuita continua disponível.", invoiceId, ct);
}

public sealed class CustomerHealthService
{
    public CustomerHealthScore Calculate(Guid clientId, bool onboardingCompleted, bool activeLast7Days, bool hasActiveHabits, bool hasRecentCompletions, bool paymentOk, bool hasCriticalTicket, bool overdue, bool benefitsBlocked, bool inactive15Days)
    {
        var score = 0; var signals = new List<string>();
        void Add(bool condition, int points, string signal) { if (condition) { score += points; signals.Add(signal); } }
        Add(onboardingCompleted, 20, "Onboarding concluído"); Add(activeLast7Days, 20, "Usuário ativo nos últimos 7 dias"); Add(hasActiveHabits, 20, "Hábitos ativos"); Add(hasRecentCompletions, 15, "Conclusões recentes"); Add(paymentOk, 15, "Pagamento em dia"); Add(!hasCriticalTicket, 10, "Sem tickets críticos");
        if (overdue) { score -= 30; signals.Add("Inadimplente"); } if (benefitsBlocked) { score -= 30; signals.Add("Benefícios bloqueados"); } if (inactive15Days) { score -= 20; signals.Add("Sem acesso há 15 dias"); } if (!onboardingCompleted) { score -= 15; signals.Add("Onboarding incompleto"); } if (hasCriticalTicket) { score -= 10; signals.Add("Ticket crítico aberto"); }
        score = Math.Clamp(score, 0, 100); var status = score >= 75 ? "Saudável" : score >= 45 ? "Atenção" : "Risco";
        return new CustomerHealthScore(clientId, score, status, signals);
    }
}
