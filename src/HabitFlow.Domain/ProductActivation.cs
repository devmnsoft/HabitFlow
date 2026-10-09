using HabitFlow.Shared;

namespace HabitFlow.Domain;

public enum ImplantationStepStatus { Pendente, EmAndamento, Concluido, Bloqueado, Ignorado }

public sealed record ImplantationOverride(string StepCode, ImplantationStepStatus Status, string? Reason);

public sealed record ImplantationFacts(
    bool OrganizationFilled,
    bool PlanSelected,
    bool AdminCreated,
    bool UsersInvited,
    bool FirstHabitCreated,
    bool TemplatesSelected,
    bool NotificationsConfigured,
    bool PlanAllowsAi,
    bool? AiConfigured,
    bool BillingReviewed,
    bool? SupportConfigured,
    IReadOnlyList<ImplantationOverride> Overrides);

public sealed record ImplantationStep(
    string Code, string Title, ImplantationStepStatus Status, string Help, string ActionUrl, string? Reason)
{
    public bool IsTerminal => Status is ImplantationStepStatus.Concluido or ImplantationStepStatus.Ignorado or ImplantationStepStatus.Bloqueado;
    public string StatusLabel => Status switch
    {
        ImplantationStepStatus.EmAndamento => "Em andamento",
        ImplantationStepStatus.Concluido => "Concluído",
        ImplantationStepStatus.Bloqueado => "Bloqueado",
        ImplantationStepStatus.Ignorado => "Ignorado",
        _ => "Pendente"
    };
}

public static class ImplantationChecklist
{
    public static readonly string[] CanonicalSteps = 
    [
        "create_account", "confirm_email", "complete_profile", "first_habit",
        "configure_reminder", "open_report", "test_ai", "configure_billing",
        "invite_member", "finish_checklist"
    ];

    public static readonly string[] Codes =
    [
        "create_account", "confirm_email", "complete_profile", "first_habit",
        "configure_reminder", "open_report", "test_ai", "configure_billing",
        "invite_member", "finish_checklist",
        "organization", "plan", "admin", "users", "habit", "templates", "notifications", "ai", "billing", "support"
    ];

    public static IReadOnlyList<ImplantationStep> Evaluate(ImplantationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return
        [
            Step("organization", "Concluir perfil e dados da organização", facts.OrganizationFilled, "Complete os dados da empresa e dados de contato.", "/admin/company", facts),
            Step("plan", "Confirmar e-mail e plano", facts.PlanSelected, "Revise o plano contratado e confirme o e-mail do administrador.", "/plans", facts),
            Step("admin", "Criar conta e administrador", facts.AdminCreated, "A conta precisa de um administrador do tenant provisionado.", "/admin/users", facts),
            Step("users", "Convidar membros de equipe", facts.UsersInvited, "Convide os membros da sua equipe para colaborar.", "/admin/users/invite", facts),
            Step("habit", "Criar primeiro hábito", facts.FirstHabitCreated, "Crie um hábito próprio ou selecione um modelo da biblioteca.", "/habits", facts),
            Step("templates", "Abrir relatório e modelos", facts.TemplatesSelected, "Visualize o relatório de progresso ou escolha modelos de rotina.", "/reports", facts),
            Step("notifications", "Configurar lembrete", facts.NotificationsConfigured, "Defina horários de lembretes e notificações.", "/notifications/preferences", facts),
            Ai(facts),
            Step("billing", "Configurar billing", facts.BillingReviewed, "Revise a assinatura comercial sem expor dados confidenciais.", "/admin/company#billing", facts),
            Support(facts)
        ];
    }

    public static bool CanFinish(IReadOnlyList<ImplantationStep> steps) =>
        steps.Count > 0 && steps.All(step => step.IsTerminal);

    public static Result Ignore(string stepCode, string? reason)
    {
        if (!Codes.Contains(stepCode)) return Result.Failure("onboarding.step_invalid", "Etapa de implantação desconhecida.");
        var text = reason?.Trim() ?? "";
        if (text.Length < 5 || text.Length > 500) return Result.Failure("onboarding.reason_required", "Informe uma justificativa entre 5 e 500 caracteres.");
        return Result.Success();
    }

    private static ImplantationStep Step(string code, string title, bool done, string help, string url, ImplantationFacts facts)
    {
        var status = done ? ImplantationStepStatus.Concluido : Override(code, facts) ?? ImplantationStepStatus.Pendente;
        return new(code, title, status, help, url, Reason(code, facts, status));
    }

    private static ImplantationStep Ai(ImplantationFacts facts)
    {
        const string code = "ai";
        if (!facts.PlanAllowsAi)
            return new(code, "Testar IA", ImplantationStepStatus.Bloqueado, "O plano atual não libera o assistente externo.", "/plans", "Plano sem cota de IA.");
        if (facts.AiConfigured is null)
            return new(code, "Testar IA", Override(code, facts) ?? ImplantationStepStatus.Pendente, "A leitura da configuração de IA não está disponível.", "/admin/ai", "não disponível");
        if (facts.AiConfigured.Value)
            return new(code, "Testar IA", ImplantationStepStatus.Concluido, "Assistente e assistente inteligente ativos.", "/admin/ai", null);
        return new(code, "Testar IA", Override(code, facts) ?? ImplantationStepStatus.Pendente, "A IA externa está desligada. Ative ou continue com o guia interno.", "/admin/ai", null);
    }

    private static ImplantationStep Support(ImplantationFacts facts)
    {
        const string code = "support";
        if (facts.SupportConfigured is null)
            return new(code, "Concluir checklist e suporte", Override(code, facts) ?? ImplantationStepStatus.Pendente, "O contato de suporte não está disponível nesta leitura.", "/admin/support", "não disponível");
        if (facts.SupportConfigured.Value)
            return new(code, "Concluir checklist e suporte", ImplantationStepStatus.Concluido, "O canal de suporte e implantação está configurado.", "/admin/support", null);
        return new(code, "Concluir checklist e suporte", Override(code, facts) ?? ImplantationStepStatus.Pendente, "Confirme o suporte e encerre o checklist.", "/admin/support", null);
    }

    private static ImplantationStepStatus? Override(string code, ImplantationFacts facts)
    {
        var item = facts.Overrides.FirstOrDefault(step => step.StepCode == code || MatchAlias(step.StepCode, code));
        if (item is null) return null;
        if (item.Status == ImplantationStepStatus.Ignorado && (item.Reason?.Trim().Length ?? 0) < 5) return null;
        if (item.Status is ImplantationStepStatus.Ignorado or ImplantationStepStatus.EmAndamento or ImplantationStepStatus.Concluido) return item.Status;
        return null;
    }

    private static bool MatchAlias(string code1, string code2)
    {
        if ((code1 == "organization" && code2 == "complete_profile") || (code2 == "organization" && code1 == "complete_profile")) return true;
        if ((code1 == "plan" && code2 == "confirm_email") || (code2 == "plan" && code1 == "confirm_email")) return true;
        if ((code1 == "admin" && code2 == "create_account") || (code2 == "admin" && code1 == "create_account")) return true;
        if ((code1 == "users" && code2 == "invite_member") || (code2 == "users" && code1 == "invite_member")) return true;
        if ((code1 == "habit" && code2 == "first_habit") || (code2 == "habit" && code1 == "first_habit")) return true;
        if ((code1 == "templates" && code2 == "open_report") || (code2 == "templates" && code1 == "open_report")) return true;
        if ((code1 == "notifications" && code2 == "configure_reminder") || (code2 == "notifications" && code1 == "configure_reminder")) return true;
        if ((code1 == "ai" && code2 == "test_ai") || (code2 == "ai" && code1 == "test_ai")) return true;
        if ((code1 == "billing" && code2 == "configure_billing") || (code2 == "billing" && code1 == "configure_billing")) return true;
        if ((code1 == "support" && code2 == "finish_checklist") || (code2 == "support" && code1 == "finish_checklist")) return true;
        return false;
    }

    private static string? Reason(string code, ImplantationFacts facts, ImplantationStepStatus status) =>
        status == ImplantationStepStatus.Ignorado
            ? facts.Overrides.FirstOrDefault(step => step.StepCode == code || MatchAlias(step.StepCode, code))?.Reason
            : null;
}

public sealed record TemplateUseDecision(bool Allowed, string? Code, string Message);

public static class HabitTemplateAccess
{
    public static readonly string[] Categories =
    [
        "Produtividade", "Saúde", "Estudos", "Bem-estar", "Rotina matinal", "Rotina noturna",
        "Atividade física", "Leitura", "Foco", "Organização", "Corporativo", "Onboarding de equipe"
    ];

    public static bool CanOwnTemplates(string? planCode) => Rank(planCode) >= Rank(PlanCodes.Team);

    public static bool MeetsMinimumPlan(string? userPlan, string? minimumPlan) => Rank(userPlan) >= Rank(minimumPlan);

    public static TemplateUseDecision CanEdit(bool isSuperAdmin, Guid? templateClientId, Guid? actorClientId)
    {
        if (isSuperAdmin) return new(true, null, "Edição permitida para o SuperAdmin.");
        if (templateClientId is null) return new(false, "template.global_protected", "Um template global só pode ser alterado pelo SuperAdmin.");
        if (actorClientId is null || templateClientId != actorClientId) return new(false, "template.tenant_forbidden", "Este template pertence a outro tenant.");
        return new(true, null, "Edição permitida no template do tenant.");
    }

    public static TemplateUseDecision CanUse(Guid? templateClientId, Guid actorClientId, string? userPlan, string? minimumPlan, int activeHabits, int? limit, bool alreadyExists, bool duplicateConfirmed)
    {
        if (actorClientId == Guid.Empty) return new(false, "template.tenant_required", "A conta é obrigatória.");
        if (templateClientId is Guid owner && owner != actorClientId) return new(false, "template.tenant_forbidden", "Este template pertence a outro tenant.");
        if (!MeetsMinimumPlan(userPlan, minimumPlan)) return new(false, "template.plan_required", "Seu plano não inclui este template.");
        if (limit is >= 0 && activeHabits >= limit) return new(false, "template.habit_limit", "Seu plano não possui espaço para outro hábito ativo.");
        if (alreadyExists && !duplicateConfirmed) return new(false, "template.duplicate_confirmation", "Este hábito já existe. Confirme para criar uma variação.");
        return new(true, null, alreadyExists ? "Variação confirmada." : "Template disponível.");
    }

    public static int Rank(string? planCode) => (planCode ?? "").Trim().ToLowerInvariant() switch
    {
        "free" => 0,
        "ritmo" or "premium" => 1,
        "team" or "evolucao" => 2,
        "enterprise" => 3,
        _ => 0
    };
}

public sealed record TenantActivationRow(
    Guid ClientId, string Name, string Status, string Plan, string SubscriptionStatus, string PaymentStatus, string BenefitsStatus,
    int ActiveUsers, int InactiveUsers, int Habits, int Completions7Days, bool CompletionsKnown,
    int OpenTickets, bool TicketsKnown, int AiRequests7Days, bool AiKnown, bool HasAdmin,
    bool CompanyDone, bool BillingDone, bool UsersInvited, bool FirstHabit, bool PlanReviewed,
    bool Active7Days, bool Active15Days, bool NotificationsConfigured, bool? SupportConfigured, bool? AiConfigured,
    int TemplateUses, bool TemplateUsesKnown, bool NotificationsKnown);

public sealed record CustomerMetric(string Label, string Display, bool Available, string? Pendency);

public sealed record CustomerHealthRow(Guid ClientId, string Name, string Plan, string Status, int? Score, string Health, IReadOnlyList<string> Signals, IReadOnlyList<string> Pending);

public sealed record CustomerSuccessPage(IReadOnlyList<CustomerMetric> Indicators, IReadOnlyList<CustomerHealthRow> Clients, IReadOnlyList<string> Pendencies);

public static class CustomerSuccessBoard
{
    public static CustomerSuccessPage Build(IReadOnlyList<TenantActivationRow> rows, bool ticketsRead, bool aiRead, bool completionsRead)
    {
        var clients = rows.Select(Score).ToList();
        var pending = new List<string>();
        if (!ticketsRead) pending.Add("Chamados de suporte não disponíveis nesta leitura.");
        if (!aiRead) pending.Add("Uso de IA não disponível nesta leitura.");
        if (!completionsRead) pending.Add("Taxa de conclusão não disponível nesta leitura.");
        pending.Add("Limite dos planos pagos não é calculado aqui. Apenas o Free usa o teto de 5 hábitos.");

        CustomerMetric Count(string label, int value) => new(label, value.ToString(), true, null);
        CustomerMetric Missing(string label, string pendency) => new(label, "não disponível", false, pendency);

        var indicators = new List<CustomerMetric>
        {
            Count("Clientes ativos", rows.Count(row => row.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) && row.BenefitsStatus is not "PremiumBlocked" and not "EnterpriseBlocked")),
            Count("Clientes em trial", rows.Count(row => row.SubscriptionStatus.Equals("Trial", StringComparison.OrdinalIgnoreCase))),
            Count("Clientes bloqueados", rows.Count(row => row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) || row.BenefitsStatus.Contains("Blocked", StringComparison.OrdinalIgnoreCase))),
            Count("Pagamento pendente", rows.Count(PaymentPending)),
            Count("Tenants sem uso", rows.Count(row => row.Habits == 0 && row.ActiveUsers == 0)),
            Count("Usuários ativos", rows.Sum(row => row.ActiveUsers)),
            Count("Usuários inativos", rows.Sum(row => row.InactiveUsers)),
            Count("Hábitos criados", rows.Sum(row => row.Habits)),
            completionsRead ? new CustomerMetric("Taxa de conclusão", Completion(rows), true, null) : Missing("Taxa de conclusão", "Conclusões dos últimos 7 dias indisponíveis."),
            aiRead ? Count("Uso de IA", rows.Sum(row => row.AiRequests7Days)) : Missing("Uso de IA", "Eventos de IA indisponíveis."),
            Count("Limite Free atingido", rows.Count(row => IsFree(row.Plan) && row.Habits >= AppConstants.FreePlanHabitLimit)),
            ticketsRead ? Count("Chamados abertos", rows.Sum(row => row.OpenTickets)) : Missing("Chamados abertos", "Fila de suporte indisponível."),
            Count("Risco de churn", clients.Count(row => row.Health == "Risco"))
        };
        return new(indicators, clients, pending);
    }

    public static CustomerHealthRow Score(TenantActivationRow row)
    {
        var pending = new List<string>();
        var signals = new List<string>();
        var score = 0;
        var max = 0;
        void Add(bool? value, int points, string label)
        {
            if (value is null) { pending.Add(label + ": não disponível"); return; }
            max += points;
            if (value.Value) { score += points; signals.Add(label); }
        }
        Add(row.CompanyDone && row.PlanReviewed && row.HasAdmin, 20, "Ativação");
        Add(row.Active7Days, 20, "Uso recente");
        Add(row.ActiveUsers > 0, 15, "Usuários ativos");
        Add(row.CompletionsKnown ? row.Completions7Days > 0 : null, 15, "Conclusão de hábitos");
        Add(!PaymentPending(row) && !Overdue(row), 15, "Sem pendência financeira");
        Add(row.TicketsKnown ? row.OpenTickets == 0 : null, 15, "Sem chamados abertos");
        if (Overdue(row)) { score -= 30; signals.Add("Pendência financeira"); }
        if (row.BenefitsStatus.Contains("Blocked", StringComparison.OrdinalIgnoreCase)) { score -= 30; signals.Add("Benefícios bloqueados"); }
        if (!row.Active15Days) { score -= 20; signals.Add("Sem uso há 15 dias"); }
        if (IsFree(row.Plan) && row.Habits >= AppConstants.FreePlanHabitLimit) { score -= 10; signals.Add("Limite Free atingido"); }
        if (!row.TicketsKnown) pending.Add("Chamados: não disponível");
        if (!row.CompletionsKnown) pending.Add("Conclusão: não disponível");
        if (!row.AiKnown) pending.Add("Uso de IA: não disponível");
        if (!IsFree(row.Plan)) pending.Add("Limite do plano pago: não disponível");
        score = Math.Clamp(score, 0, 100);

        var isBlocked = row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
                     || row.BenefitsStatus.Contains("Blocked", StringComparison.OrdinalIgnoreCase);
        var isPaymentIssue = Overdue(row) || PaymentPending(row);
        var isTrialEnding = row.SubscriptionStatus.Equals("Trial", StringComparison.OrdinalIgnoreCase) && !row.Active15Days;

        var health = max < 45 ? "não disponível" :
                     score >= 75 ? "Saudável" :
                     score >= 45 ? "Atenção" : "Risco";

        return new(row.ClientId, row.Name, row.Plan, row.Status, max < 45 ? null : score, health, signals, pending);
    }

    public static bool PaymentPending(TenantActivationRow row) =>
        row.PaymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)
        || row.SubscriptionStatus.Equals("PaymentPending", StringComparison.OrdinalIgnoreCase)
        || row.SubscriptionStatus.Equals("PastDue", StringComparison.OrdinalIgnoreCase);

    public static bool Overdue(TenantActivationRow row) =>
        row.PaymentStatus.Equals("Overdue", StringComparison.OrdinalIgnoreCase)
        || row.SubscriptionStatus.Equals("PastDue", StringComparison.OrdinalIgnoreCase);

    private static bool IsFree(string plan) => plan.Equals("Free", StringComparison.OrdinalIgnoreCase) || plan.Equals("free", StringComparison.OrdinalIgnoreCase);

    private static string Completion(IReadOnlyList<TenantActivationRow> rows)
    {
        var habits = rows.Sum(row => row.Habits);
        if (habits == 0) return "não disponível";
        var rate = Math.Min(100, rows.Sum(row => row.Completions7Days) * 100 / (habits * 7));
        return rate + "%";
    }
}

public sealed record HomologationQuery(DateOnly? From, DateOnly? To, string? TenantName, string? Plan, string? Status, Guid? ClientId);

public sealed record HomologationReport(IReadOnlyList<TenantActivationRow> Rows, IReadOnlyList<string> Pendencies);

public static class HomologationAccess
{
    public static bool CanView(string? role) => role is "SuperAdmin" or "Admin" or "TenantAdmin" or "TenantOwner";

    public static HomologationQuery Scope(string? role, Guid? actorClientId, HomologationQuery requested)
    {
        if (string.Equals(role, "SuperAdmin", StringComparison.Ordinal)) return requested with { ClientId = null };
        return requested with { ClientId = actorClientId };
    }
}

public static class HomologationCsv
{
    public static string Write(HomologationReport report)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Cliente;Status;Plano;Assinatura;Pagamento;Usuarios ativos;Usuarios inativos;Habitos;Conclusoes 7d;Uso de IA 7d;Chamados;Templates usados;Risco");
        foreach (var row in report.Rows)
        {
            var health = CustomerSuccessBoard.Score(row);
            builder.AppendLine(string.Join(';',
                Cell(row.Name), Cell(row.Status), Cell(row.Plan), Cell(row.SubscriptionStatus), Cell(row.PaymentStatus),
                row.ActiveUsers.ToString(), row.InactiveUsers.ToString(), row.Habits.ToString(),
                row.CompletionsKnown ? row.Completions7Days.ToString() : "não disponível",
                row.AiKnown ? row.AiRequests7Days.ToString() : "não disponível",
                row.TicketsKnown ? row.OpenTickets.ToString() : "não disponível",
                row.TemplateUsesKnown ? row.TemplateUses.ToString() : "não disponível",
                Cell(health.Health)));
        }
        return builder.ToString();
    }

    public static string Cell(string? value)
    {
        var text = (value ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        return text.Length > 0 && "=+-@".Contains(text[0]) ? "'" + text : text;
    }
}

public sealed record AlertEvaluation(string Code, string Title, bool? Condition, bool PreferenceAllows, bool CanCreateInApp, string ChannelNote);

public static class NotificationDispatchPolicy
{
    public static IReadOnlyList<AlertEvaluation> Evaluate(AlertSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return
        [
            Item("habit_reminder", "Lembrete de hábito", signals.HabitDue, signals.HabitRemindersEnabled, signals),
            Item("goal_near", "Meta próxima", signals.GoalNear, signals.InternalEnabled, signals),
            Item("inactivity", "Inatividade", signals.Inactive, signals.InternalEnabled, signals),
            Item("weekly_summary", "Resumo semanal", signals.WeeklyDue, signals.WeeklySummaryEnabled, signals),
            Item("plan_limit", "Limite de plano", signals.PlanLimitReached, signals.InternalEnabled, signals),
            Item("subscription", "Assinatura", signals.SubscriptionAlert, signals.InternalEnabled, signals),
            Item("low_adhesion", "Baixa adesão", signals.LowAdhesion, signals.InternalEnabled && signals.IsTenantAdmin, signals),
            Item("churn_risk", "Cliente em risco", signals.ChurnRisk, signals.InternalEnabled && signals.IsSuperAdmin, signals)
        ];
    }

    public static string Channels(bool emailConfigured, bool whatsAppConfigured)
    {
        var note = "No aplicativo, quando a preferência permitir.";
        note += emailConfigured
            ? " E-mail transacional configurado; este fluxo não dispara e-mail sozinho."
            : " E-mail não configurado. Use o contato comercial@mnsoft.com.br.";
        note += whatsAppConfigured
            ? " WhatsApp só como contato manual, sem envio automático."
            : " WhatsApp não integrado. Use o contato manual.";
        return note;
    }

    private static AlertEvaluation Item(string code, string title, bool? condition, bool preference, AlertSignals signals)
    {
        var can = condition == true && preference;
        return new(code, title, condition, preference, can, Channels(signals.EmailConfigured, signals.WhatsAppConfigured));
    }
}

public sealed record AlertSignals(
    bool? HabitDue, bool? GoalNear, bool? Inactive, bool? WeeklyDue, bool? PlanLimitReached, bool? SubscriptionAlert,
    bool? LowAdhesion, bool? ChurnRisk, bool HabitRemindersEnabled, bool WeeklySummaryEnabled, bool InternalEnabled,
    bool IsTenantAdmin, bool IsSuperAdmin, bool EmailConfigured, bool WhatsAppConfigured);

public static class AppliedAssistantGuide
{
    public static string? SuggestHabits(string? goal)
    {
        var text = Fold(goal);
        if (text.Length == 0) return null;
        if (text.Contains("estud", StringComparison.Ordinal) || text.Contains("aprend", StringComparison.Ordinal))
            return "Para estudo, a biblioteca tem \"Estudar 30 minutos\" e \"Revisar anotações\". Comece por um. Isso não garante resultado.";
        if (text.Contains("saud", StringComparison.Ordinal) || text.Contains("agua", StringComparison.Ordinal))
            return "Para saúde cotidiana, a biblioteca tem \"Beber água\" e \"Caminhar 20 minutos\". Não é orientação médica.";
        if (text.Contains("foco", StringComparison.Ordinal) || text.Contains("trabal", StringComparison.Ordinal))
            return "Para foco, use \"Bloco de foco de 25 minutos\" ou \"Planejar o dia\". Ajuste o horário à sua rotina.";
        if (text.Contains("equipe", StringComparison.Ordinal) || text.Contains("corporat", StringComparison.Ordinal))
            return "Para equipe, veja \"Alinhar a equipe em 10 minutos\" e \"Receber um colega novo\" se o plano permitir templates corporativos.";
        return "Escolha um hábito pequeno na biblioteca: \"Planejar o dia\" ou \"Beber água\". O assistente não promete resultado.";
    }

    public static string? SuggestTemplate(string? message)
    {
        var text = Fold(message);
        if (!text.Contains("template", StringComparison.Ordinal) && !text.Contains("modelo", StringComparison.Ordinal)) return null;
        if (text.Contains("manha", StringComparison.Ordinal)) return "O template \"Começar o dia com uma prioridade\" está na categoria Rotina matinal.";
        if (text.Contains("noite", StringComparison.Ordinal)) return "O template \"Encerrar o dia sem telas\" está na categoria Rotina noturna.";
        if (text.Contains("equipe", StringComparison.Ordinal)) return "O template \"Receber um colega novo\" está em Onboarding de equipe e exige plano Team ou Enterprise.";
        return "Abra a biblioteca e filtre por categoria. Templates globais não podem ser editados pelo tenant.";
    }

    private static string Fold(string? value)
    {
        var lower = (value ?? "").ToLowerInvariant();
        return lower.Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("õ", "o").Replace("ô", "o")
            .Replace("ú", "u").Replace("ç", "c");
    }
}
