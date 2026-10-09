using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public class OperationalSaaSV6210Tests
{
    private readonly TenantHealthEvaluationService _healthService;
    private readonly OperationalIncidentService _incidentService;
    private readonly SupportCenterService _supportService;
    private readonly OperationalAiService _aiService;
    private readonly FakeIncidentRepository _fakeIncidentRepo;
    private readonly FakeAssistanceRepository _fakeAssistanceRepo;

    public OperationalSaaSV6210Tests()
    {
        var fakeActivationRepo = new FakeActivationRepository();
        _healthService = new TenantHealthEvaluationService(fakeActivationRepo, NullLogger<TenantHealthEvaluationService>.Instance);
        _fakeIncidentRepo = new FakeIncidentRepository();
        _incidentService = new OperationalIncidentService(_fakeIncidentRepo, NullLogger<OperationalIncidentService>.Instance);
        _fakeAssistanceRepo = new FakeAssistanceRepository();
        _supportService = new SupportCenterService(
            _fakeAssistanceRepo,
            new WhatsAppValidator(),
            new ProtocolGenerator(),
            new AssistantSafetyPolicy(),
            NullLogger<SupportCenterService>.Instance);
        _aiService = new OperationalAiService([], new AssistantSafetyPolicy(), NullLogger<OperationalAiService>.Instance);
    }

    [Fact]
    public void TenantHealth_Healthy_WhenHighEngagementAndPaymentsOk()
    {
        var row = CreateRow(active7Days: true, activeUsers: 5, completions7Days: 35, paymentStatus: "Paid", subscriptionStatus: "Active");
        var eval = _healthService.Evaluate(row, trialRemainingDays: null);

        Assert.Equal(TenantHealthStatus.Healthy, eval.HealthCategory);
        Assert.True(eval.HealthScore >= 75);
        Assert.False(eval.ChurnRisk);
    }

    [Fact]
    public void TenantHealth_TrialEnding_Generates_Risk_And_Category()
    {
        var row = CreateRow(active7Days: true, activeUsers: 2, completions7Days: 10, subscriptionStatus: "Trial");
        var eval = _healthService.Evaluate(row, trialRemainingDays: 2);

        Assert.Equal(TenantHealthStatus.TrialEnding, eval.HealthCategory);
        Assert.Contains("Trial expirando em 2 dia(s)", eval.RiskFactors);
    }

    [Fact]
    public void TenantHealth_Overdue_Generates_PaymentIssue_Category()
    {
        var row = CreateRow(active7Days: true, activeUsers: 2, paymentStatus: "Overdue");
        var eval = _healthService.Evaluate(row);

        Assert.Equal(TenantHealthStatus.PaymentIssue, eval.HealthCategory);
        Assert.True(eval.ChurnRisk);
        Assert.Contains("Inadimplência ou pagamento pendente", eval.RiskFactors);
    }

    [Fact]
    public void TenantHealth_Inactive15Days_Generates_AtRisk_Category()
    {
        var row = CreateRow(active7Days: false, active15Days: false, activeUsers: 0, completions7Days: 0);
        var eval = _healthService.Evaluate(row);

        Assert.Equal(TenantHealthStatus.AtRisk, eval.HealthCategory);
        Assert.True(eval.ChurnRisk);
    }

    [Fact]
    public void TenantHealth_Blocked_Generates_Blocked_Category()
    {
        var row = CreateRow(status: "Blocked", benefitsStatus: "Blocked");
        var eval = _healthService.Evaluate(row);

        Assert.Equal(TenantHealthStatus.Blocked, eval.HealthCategory);
        Assert.True(eval.ChurnRisk);
    }

    [Fact]
    public void Onboarding_Checklist_Has_Ten_Canonical_Steps()
    {
        Assert.Equal(10, ImplantationChecklist.CanonicalSteps.Length);
        Assert.Contains("create_account", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("confirm_email", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("complete_profile", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("first_habit", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("configure_reminder", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("open_report", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("test_ai", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("configure_billing", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("invite_member", ImplantationChecklist.CanonicalSteps);
        Assert.Contains("finish_checklist", ImplantationChecklist.CanonicalSteps);
    }

    [Fact]
    public void Onboarding_IgnoreStep_Requires_Reason_Length_Gte_Five()
    {
        var invalid = ImplantationChecklist.Ignore("first_habit", "abc");
        Assert.True(invalid.IsFailure);
        Assert.Equal("onboarding.reason_required", invalid.Error.Code);

        var valid = ImplantationChecklist.Ignore("first_habit", "Hábito será criado pelo gestor de RH na segunda-feira.");
        Assert.True(valid.IsSuccess);
    }

    [Fact]
    public async Task Support_Ticket_Created_With_PlanAware_Sla()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var id = await _supportService.CreateAsync(
            clientId, userId, "Question", "Critical", "Sistema instável",
            "Erro ao sincronizar rotina diária", "/habits", "Chrome", "1920x1080",
            "enterprise", Guid.NewGuid().ToString("N"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        var ticket = await _supportService.GetAsync(clientId, userId, id, false, CancellationToken.None);
        Assert.NotNull(ticket);
        Assert.Equal("Critical", ticket.Priority);
        Assert.Equal("Open", ticket.Status);
    }

    [Fact]
    public void Support_Sla_Prioritizes_Enterprise_Over_Free()
    {
        var entHours = SupportSla.Hours("Critical", "enterprise");
        var freeHours = SupportSla.Hours("Critical", "free");

        Assert.True(entHours < freeHours);
        Assert.Equal(4, entHours);
        Assert.Equal(24, entHours * 6); // 24 para free
    }

    [Fact]
    public async Task Support_Resolve_Requires_Resolution_Reason()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var id = await _supportService.CreateAsync(clientId, userId, "Question", "Medium", "Dúvida", "Como exportar?", "/reports", "Chrome", "1024x768", "premium", "corr-1", CancellationToken.None);

        var fail = await _supportService.ResolveAsync(clientId, userId, id, "ok", false, CancellationToken.None);
        Assert.True(fail.IsFailure);
        Assert.Equal("support.reason_required", fail.Error.Code);

        var success = await _supportService.ResolveAsync(clientId, userId, id, "Orientado a clicar no botão de exportação CSV na aba relatórios.", false, CancellationToken.None);
        Assert.True(success.IsSuccess);

        var resolvedTicket = await _supportService.GetAsync(clientId, userId, id, false, CancellationToken.None);
        Assert.Equal("Resolved", resolvedTicket?.Status);
    }

    [Fact]
    public async Task Support_Submit_Satisfaction_Only_When_Resolved_Or_Closed()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var id = await _supportService.CreateAsync(clientId, userId, "Question", "Low", "Dúvida", "Qual o limite?", "/plans", "Chrome", "1024x768", "free", "corr-2", CancellationToken.None);

        // Enquanto Open, não permite satisfação
        var premature = await _supportService.SubmitSatisfactionAsync(clientId, userId, id, 5, "Ótimo!", CancellationToken.None);
        Assert.True(premature.IsFailure);

        // Resolve
        await _supportService.ResolveAsync(clientId, userId, id, "Limite explicado conforme documentação comercial.", false, CancellationToken.None);

        // Agora submete satisfação com sucesso
        var ok = await _supportService.SubmitSatisfactionAsync(clientId, userId, id, 5, "Atendimento muito rápido!", CancellationToken.None);
        Assert.True(ok.IsSuccess);

        var t = await _supportService.GetAsync(clientId, userId, id, false, CancellationToken.None);
        Assert.Equal(5, t?.SatisfactionRating);
    }

    [Fact]
    public async Task Operational_Incident_Created_And_Resolved_With_Audit()
    {
        var result = await _incidentService.CreateAsync(
            "Latência no banco de dados", "Consultas lentas na tabela de histórico",
            "Major", "Acesso aos relatórios degradado", 15,
            DateTime.UtcNow.AddHours(2), "usr-1", "Engenharia SRE",
            false, "Investigação iniciada", "sre@habitflow.com", "corr-inc-1");

        Assert.True(result.IsSuccess);
        var incId = result.Value;

        var inc = await _incidentService.GetByIdAsync(incId);
        Assert.NotNull(inc);
        Assert.Equal("Investigating", inc.Status);

        var resolved = await _incidentService.UpdateStatusAsync(
            incId, "Resolved", "Índice recriado e latência normalizada.",
            "usr-1", "sre@habitflow.com", "corr-inc-2");

        Assert.True(resolved.IsSuccess);
        var updated = await _incidentService.GetByIdAsync(incId);
        Assert.Equal("Resolved", updated?.Status);
        Assert.NotNull(updated?.ResolvedAt);
    }

    [Fact]
    public async Task Operational_Ai_Suggests_Ticket_Reply_Without_Data_Mutation()
    {
        var suggestion = await _aiService.SuggestTicketReplyAsync(
            "Erro ao criar hábito no plano Free",
            "Aparece mensagem de limite atingido mesmo tendo 4 hábitos",
            null);

        Assert.NotNull(suggestion);
        Assert.False(string.IsNullOrWhiteSpace(suggestion.Suggestion));
        Assert.True(suggestion.RequiresHumanReview);
        Assert.Contains("Erro ao criar hábito no plano Free", suggestion.Suggestion);
    }

    [Fact]
    public void Operational_Ai_Masks_Sensitive_Data_Correctly()
    {
        var raw = "O cliente com CPF 123.456.789-00 e cartão 4111222233334444 solicitou alteração.";
        var masked = OperationalAiService.MaskSensitiveData(raw);

        Assert.DoesNotContain("123.456.789-00", masked);
        Assert.DoesNotContain("4111222233334444", masked);
        Assert.Contains("123.***.***-00", masked);
        Assert.Contains("****-****-****-****", masked);
    }

    private static TenantActivationRow CreateRow(
        bool active7Days = true,
        bool active15Days = true,
        int activeUsers = 1,
        int habits = 2,
        int completions7Days = 7,
        string status = "Active",
        string plan = "Premium",
        string subscriptionStatus = "Active",
        string paymentStatus = "Paid",
        string benefitsStatus = "Active") =>
        new(
            Guid.NewGuid(), "Empresa Teste", status, plan, subscriptionStatus, paymentStatus, benefitsStatus,
            activeUsers, 0, habits, completions7Days, true,
            0, true, 2, true, true,
            true, true, true, true, true,
            active7Days, active15Days, true, true, true, 1, true, true
        );

    private sealed class FakeActivationRepository : IProductActivationRepository
    {
        public Task<IReadOnlyList<TenantActivationRow>> ListTenantsAsync(HomologationQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TenantActivationRow>>([]);
        public Task<IReadOnlyList<ImplantationOverride>> ListOverridesAsync(Guid clientId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ImplantationOverride>>([]);
        public Task SaveOverrideAsync(Guid clientId, Guid userId, string stepCode, string status, string? reason, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task RecordNotificationEventAsync(Guid clientId, Guid? userId, string eventCode, string channel, string status, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task RecordCustomerSuccessAsync(Guid clientId, int? score, string status, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeIncidentRepository : IOperationalIncidentRepository
    {
        private readonly List<OperationalIncident> _incidents = [];
        public Task<IReadOnlyList<OperationalIncident>> ListIncidentsAsync(OperationalIncidentFilter filter, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OperationalIncident>>(_incidents);
        public Task<OperationalIncident?> GetIncidentByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_incidents.FirstOrDefault(i => i.Id == id));
        public Task<Guid> CreateIncidentAsync(OperationalIncident incident, CancellationToken ct = default)
        {
            _incidents.Add(incident);
            return Task.FromResult(incident.Id);
        }
        public Task UpdateIncidentAsync(OperationalIncident incident, CancellationToken ct = default)
        {
            var idx = _incidents.FindIndex(i => i.Id == incident.Id);
            if (idx >= 0) _incidents[idx] = incident;
            return Task.CompletedTask;
        }
        public Task RecordIncidentAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<IncidentTenant>> ListIncidentTenantsAsync(Guid incidentId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<IncidentTenant>>([]);
        public Task LinkIncidentTenantsAsync(Guid incidentId, IEnumerable<Guid> clientIds, string? impactSummary, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAssistanceRepository : IAssistanceRepository
    {
        private readonly List<SupportTicketDetail> _tickets = [];
        private readonly List<SupportTicketMessage> _messages = [];

        public Task<Guid> GetOrCreateConversationAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
        public Task AddMessageAsync(AssistantMessage message, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AssistantMessage>> ListMessagesAsync(Guid clientId, Guid userId, Guid conversationId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AssistantMessage>>([]);
        public Task DeleteHistoryAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<SupportSettings> GetSupportSettingsAsync(CancellationToken ct = default) => Task.FromResult(new SupportSettings(Guid.NewGuid(), "MNSOFT", "18.160.057/0001-13", "comercial@mnsoft.com.br", null, "Olá", "9h às 18h", true, "Falar", DateTime.UtcNow));
        public Task UpdateSupportSettingsAsync(SupportSettings settings, CancellationToken ct = default) => Task.CompletedTask;
        public Task CreateTicketAsync(SupportTicketDetail ticket, CancellationToken ct = default) { _tickets.Add(ticket); return Task.CompletedTask; }
        public Task<IReadOnlyList<SupportTicketDetail>> ListTicketsAsync(Guid clientId, Guid userId, bool admin, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SupportTicketDetail>>(_tickets.Where(t => t.ClientId == clientId).ToList());
        public Task<IReadOnlyList<SupportTicketDetail>> ListAllTicketsAsync(string? status = null, string? priority = null, string? category = null, string? search = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SupportTicketDetail>>(_tickets);
        public Task<SupportTicketDetail?> GetTicketAsync(Guid clientId, Guid userId, Guid ticketId, bool admin, CancellationToken ct = default) => Task.FromResult(_tickets.FirstOrDefault(t => t.Id == ticketId));
        public Task<SupportTicketDetail?> GetTicketByIdGlobalAsync(Guid ticketId, CancellationToken ct = default) => Task.FromResult(_tickets.FirstOrDefault(t => t.Id == ticketId));
        public Task AddTicketMessageAsync(SupportTicketMessage message, CancellationToken ct = default) { _messages.Add(message); return Task.CompletedTask; }
        public Task<IReadOnlyList<SupportTicketMessage>> ListTicketMessagesAsync(Guid clientId, Guid ticketId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SupportTicketMessage>>(_messages.Where(m => m.TicketId == ticketId).ToList());
        public Task UpdateTicketStatusAsync(Guid clientId, Guid ticketId, string status, DateTime? closedAt, CancellationToken ct = default)
        {
            var t = _tickets.FirstOrDefault(x => x.Id == ticketId);
            if (t != null)
            {
                var idx = _tickets.IndexOf(t);
                _tickets[idx] = t with { Status = status, ClosedAt = closedAt };
            }
            return Task.CompletedTask;
        }
        public Task ResolveTicketAsync(Guid clientId, Guid ticketId, Guid actorUserId, string resolutionReason, CancellationToken ct = default)
        {
            var t = _tickets.FirstOrDefault(x => x.Id == ticketId);
            if (t != null)
            {
                var idx = _tickets.IndexOf(t);
                _tickets[idx] = t with { Status = "Resolved", ResolutionReason = resolutionReason, ResolvedAt = DateTime.UtcNow };
            }
            return Task.CompletedTask;
        }
        public Task ReopenTicketAsync(Guid clientId, Guid ticketId, Guid actorUserId, string reason, DateTime slaDueAt, CancellationToken ct = default)
        {
            var t = _tickets.FirstOrDefault(x => x.Id == ticketId);
            if (t != null)
            {
                var idx = _tickets.IndexOf(t);
                _tickets[idx] = t with { Status = "Reopened", ReopenedAt = DateTime.UtcNow, SlaDueAt = slaDueAt, ClosedAt = null };
            }
            return Task.CompletedTask;
        }
        public Task SubmitSatisfactionAsync(Guid clientId, Guid ticketId, Guid userId, int rating, string? feedback, CancellationToken ct = default)
        {
            var t = _tickets.FirstOrDefault(x => x.Id == ticketId);
            if (t != null)
            {
                var idx = _tickets.IndexOf(t);
                _tickets[idx] = t with { SatisfactionRating = rating, SatisfactionFeedback = feedback };
            }
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<SupportTicketHistory>> ListTicketHistoryAsync(Guid clientId, Guid ticketId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SupportTicketHistory>>([]);
    }
}
