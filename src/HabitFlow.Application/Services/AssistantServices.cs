using System.Diagnostics;
using System.Text.RegularExpressions;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Disabled";
    public string Model { get; set; } = "";
    public int MaxInputChars { get; set; } = 4000;
    public int MaxOutputChars { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 30;
    public bool StoreConversationHistory { get; set; } = true;
    public bool AllowHabitContext { get; set; } = true;
    public bool AllowBillingContext { get; set; } = true;
    public string DefaultMessage { get; set; } = "O assistente está indisponível no momento. Fale com o suporte da MNSOFT.";
}

public sealed record AssistantRequest(string Message, Guid ClientId, Guid UserId, string CorrelationId);
public sealed record AssistantResponse(string Message, string Provider, string SafetyStatus, string? ActionUrl = null, string? ActionLabel = null);
// Deliberately contains aggregates only: habit names, notes and corporate/private content never enter a prompt.
public sealed record AssistantUserContext(int ActiveHabits, int PausedHabits, UserPlan Plan, int Reminders);
public interface IAssistantProvider
{
    bool IsConfigured { get; }
    Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct);
}

public sealed class AssistantSafetyService
{
    private static readonly Regex SecretPattern = new(@"(?i)(password|senha|api[_ -]?key|token|secret|connection\s*string|cookie|authorization)\s*[:=]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly string[] Injection = ["ignore as instruções", "ignore previous", "prompt do sistema", "system prompt", "modo desenvolvedor", "jailbreak", "revele o prompt", "outro usuário", "outro tenant", "connection string"];
    private static readonly string[] Medical = ["diagnóstico", "autodiagnóstico", "automedicação", "qual remédio", "dose de", "suicídio", "me matar", "autoagressão"];
    private static readonly string[] LegalFinancial = ["aconselhamento jurídico", "processo judicial", "parecer jurídico", "qual ação comprar", "investimento garantido", "consultoria financeira"];
    private static readonly string[] Bypass = ["burlar o plano", "contornar o limite", "mais hábitos sem pagar", "dados de outro", "cancelar assinatura", "libera o premium", "ative o premium", "desbloqueia o", "liberar recurso"];
    private static readonly string[] HardBlock = ["burlar o plano", "contornar o limite", "mais hábitos sem pagar", "dados de outro", "libera o premium", "ative o premium", "desbloqueia o", "liberar recurso", "api key", "chave de api", "connection string", "liste todos os e-mails", "dump do banco", "exfiltre", "exportar usuários", "mostre o token", "revele a senha"];

    public bool ContainsSensitiveData(string value) => SecretPattern.IsMatch(value ?? "");
    public bool IsPromptInjection(string value) => HasAny(value, Injection);
    public bool IsOutOfScope(string value) => HasAny(value, Medical) || HasAny(value, LegalFinancial);
    public bool IsDestructive(string value) => HasAny(value, Bypass);
    public string? SecurityEvent(string value)
    {
        if (ContainsSensitiveData(value) || HasAny(value, ["api key", "chave de api", "mostre o token", "qual o token", "revele a senha", "connection string"]))
            return "ai.security.secret_request_blocked";
        if (HasAny(value, ["outro tenant", "outro usuário", "dados de outro"]))
            return "ai.security.cross_tenant_context_blocked";
        if (IsPromptInjection(value)) return "ai.security.prompt_injection_detected";
        if (HasAny(value, ["dump do banco", "liste todos os e-mails", "exfiltre", "exportar usuários"]))
            return "ai.security.data_exfiltration_blocked";
        if (HasAny(value, HardBlock)) return "ai.security.secret_request_blocked";
        return null;
    }

    public AssistantResponse? InspectInput(string value)
    {
        if (ContainsSensitiveData(value) || IsPromptInjection(value) || HasAny(value, HardBlock))
            return new("Não posso ajudar a revelar dados, segredos ou contornar regras. Posso explicar recursos do HabitFlow ou direcionar você ao suporte.", "safety", "Blocked", "/support/tickets/new", "Falar com suporte");
        if (HasAny(value, ["suicídio", "me matar", "autoagressão"]))
            return new("Sinto muito que você esteja passando por isso. Procure agora uma pessoa de confiança ou um serviço de emergência da sua região. O HabitFlow não substitui ajuda profissional.", "safety", "Crisis", "/support/tickets/new", "Falar com suporte");
        if (IsOutOfScope(value))
            return new("Posso orientar apenas sobre hábitos e o HabitFlow. Para decisões médicas, jurídicas ou financeiras, procure um profissional qualificado.", "safety", "OutOfScope", "/help", "Ver ajuda");
        return null;
    }
    public AssistantResponse InspectOutput(AssistantResponse response, int maxChars)
    {
        var safe = Sanitize(response.Message, Math.Clamp(maxChars, 100, 10000));
        if (HasAny(safe, ["resultado garantido", "garanto que", "tome o remédio", "liberei o premium", "premium ativado", "api key é", "a chave é"]))
            return response with { Message = "Posso explicar o HabitFlow. Não prometo resultado, não faço diagnóstico e não libero recurso pago.", SafetyStatus = "OutOfScope" };
        return response with { Message = safe };
    }
    public string Sanitize(string value, int maxChars = 500)
    {
        var clean = Regex.Replace(value ?? "", @"(?i)(bearer\s+)[A-Za-z0-9._~-]+", "$1[REMOVIDO]", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        clean = Regex.Replace(clean, @"(?i)(password|senha|api[_ -]?key|token|secret|cookie)\s*[:=]\s*\S+", "$1=[REMOVIDO]", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return clean.Length <= maxChars ? clean : clean[..maxChars];
    }
    private static bool HasAny(string value, IEnumerable<string> terms) => terms.Any(x => (value ?? "").Contains(x, StringComparison.OrdinalIgnoreCase));
}

// Kept as a compatibility facade for existing callers and tests.
public sealed class AssistantSafetyPolicy
{
    private readonly AssistantSafetyService inner = new();
    public bool ContainsSensitiveData(string v) => inner.ContainsSensitiveData(v);
    public bool IsPromptInjection(string v) => inner.IsPromptInjection(v);
    public bool IsOutOfScope(string v) => inner.IsOutOfScope(v);
    public bool IsDestructive(string v) => inner.IsDestructive(v);
    public string Sanitize(string v) => inner.Sanitize(v);
}

public sealed record HelpArticle(string Title, string Slug, string Category, string Question, string Answer, string[] Tags, bool Active, int Order, DateTime UpdatedAt);
public sealed class AssistantKnowledgeService
{
    private static readonly DateTime Updated = new(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc);
    private static readonly HelpArticle[] Articles =
    [
        new("O que é o HabitFlow", "sobre", "Começar", "O que é o HabitFlow?", "O HabitFlow guarda hábitos, metas, lembretes e o que você concluiu. Ele organiza a rotina. Não promete resultado.", ["habitflow","começar"], true, 1, Updated),
        new("Criar hábito", "criar-habito", "Hábitos", "Como criar um hábito?", "Abra Hábitos e escolha Criar hábito. Escreva um nome curto e a frequência. Comece com uma ação pequena.", ["criar","hábito"], true, 2, Updated),
        new("Criar meta", "criar-meta", "Metas", "Como criar uma meta?", "Abra Metas e escolha Nova meta. Defina um alvo e um prazo. Depois ligue hábitos que ajudam essa meta.", ["criar","meta"], true, 3, Updated),
        new("Lembretes", "lembretes", "Notificações", "Como configurar lembretes?", "Abra Lembretes, escolha um hábito e um horário. O aviso no navegador depende da permissão do aparelho.", ["lembrete","notificação"], true, 4, Updated),
        new("Progresso", "progresso", "Progresso", "Como acompanhar o progresso?", "Marque o feito em Meu Dia. Em Relatórios, veja a consistência do período do seu plano. O número mostra o registro, não um resultado prometido.", ["progresso","streak","relatório"], true, 5, Updated),
        new("Planos e limites", "planos", "Conta", "Qual a diferença entre planos?", "A tela Planos mostra Free, Premium, Team e Enterprise. O chat não inventa preço, não libera recurso pago e não contorna o plano.", ["free","premium","limite","plano"], true, 6, Updated),
        new("Cancelar assinatura", "cancelar-assinatura", "Conta", "Como cancelar a assinatura?", "O chat não cancela. Abra Minha assinatura e, se o botão existir, confirme Cancelar assinatura. O período já pago segue até o fim. Se o botão não aparecer, fale com o suporte.", ["cancelar","assinatura"], true, 7, Updated),
        new("Privacidade", "privacidade", "Segurança", "Meus hábitos são privados?", "Seus hábitos ficam na sua conta. O assistente não mostra dados de outro usuário nem de outro tenant. Ele usa só totais, sem nomes nem notas.", ["privacidade","lgpd"], true, 8, Updated),
        new("Programas corporativos", "corporativo", "Corporativo", "Como funcionam programas corporativos?", "Programas corporativos mostram somente informações autorizadas e agregadas aos gestores. Hábitos privados não são expostos.", ["programa","corporativo","gestor"], true, 9, Updated),
        new("Suporte", "suporte", "Ajuda", "Como falar com suporte?", "Abra Suporte e crie um chamado. Não envie senha, chave ou token. Para assunto comercial, escreva para comercial@mnsoft.com.br.", ["suporte","mnsoft","chamado"], true, 10, Updated),
        new("Boas práticas", "boas-praticas", "Hábitos", "Como melhorar minha rotina?", "Escolha poucos hábitos, um horário que caiba no dia e repita. Se a semana falhou, reduza a lista em vez de tentar recuperar tudo.", ["rotina","consistência","semana"], true, 11, Updated),
        new("Limites do plano", "limites", "Conta", "Como funcionam os limites?", "Cada plano tem um teto. O número vigente está na tela Planos. O chat não aumenta esse teto.", ["limites","cotas"], true, 12, Updated),
        new("Contato comercial", "contato", "Conta", "Qual o contato comercial?", "O contato comercial da MNSOFT é comercial@mnsoft.com.br. Preço e benefício ficam na tela Planos.", ["comercial","contato"], true, 13, Updated)
    ];
    public IReadOnlyList<HelpArticle> List(string? category = null) => Articles.Where(x => x.Active && (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.Order).ToArray();
    public HelpArticle? Get(string slug) => Articles.FirstOrDefault(x => x.Active && x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<HelpArticle> Search(string? query) => string.IsNullOrWhiteSpace(query) ? List() : Articles.Where(a => query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(t => ($"{a.Title} {a.Question} {a.Answer} {string.Join(' ', a.Tags)}").Contains(t, StringComparison.OrdinalIgnoreCase))).ToArray();
    public HelpArticle? Match(string message) => Articles.Select(a => new { Article = a, Score = a.Tags.Count(t => message.Contains(t, StringComparison.OrdinalIgnoreCase)) }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Article.Order).Select(x => x.Article).FirstOrDefault();

    public bool MatchesGuide(string message) => ResolveGuide(message) is not null;
    public bool NeedsAccount(string message) => ResolveGuide(message) is "rotina" or "evolucao" or "consistencia";

    public AssistantResponse? Guide(string message, AssistantUserContext? context)
    {
        var kind = ResolveGuide(message);
        if (kind is null) return null;
        return kind switch
        {
            "habito" => From("criar-habito", "/habits", "Abrir hábitos"),
            "rotina" => new(Routine(context), "Knowledge", "Allowed", "/my-day", "Abrir Meu Dia"),
            "meta" => From("criar-meta", "/goals", "Abrir metas"),
            "planos" => From("planos", "/plans", "Ver planos"),
            "limites" => From("limites", "/plans", "Ver planos"),
            "evolucao" => new(Evolution(context), "Knowledge", "Allowed", "/reports", "Abrir relatórios"),
            "consistencia" => new(Consistency(context), "Knowledge", "Allowed", "/reports", "Abrir relatórios"),
            "suporte" => From("suporte", "/support/tickets/new", "Abrir chamado"),
            "cancelar" => From("cancelar-assinatura", "/billing", "Abrir assinatura"),
            "privacidade" => From("privacidade", "/support", "Falar com suporte"),
            "comercial" => From("contato", "/plans", "Ver planos"),
            "sobre" => From("sobre", "/help", "Ver ajuda"),
            _ => null
        };
    }

    private AssistantResponse From(string slug, string url, string label)
    {
        var article = Get(slug);
        return new(article?.Answer ?? "Não encontrei essa informação. Fale com o suporte.", "Knowledge", "Allowed", url, label);
    }

    private static string Routine(AssistantUserContext? context)
    {
        var totals = context is null ? "Monte um dia curto." : $"Você tem {context.ActiveHabits} hábitos ativos.";
        return $"{totals} Em Meu Dia, faça só o que cabe hoje e deixe um horário fixo. Não encha a lista.";
    }

    private static string Evolution(AssistantUserContext? context)
    {
        var totals = context is null
            ? "Abra Relatórios para ver o período do seu plano."
            : $"Na sua conta há {context.ActiveHabits} hábitos ativos, {context.PausedHabits} pausados e {context.Reminders} lembretes. Plano: {context.Plan}.";
        return $"{totals} Meu Dia mostra o que foi marcado. Relatórios mostram a consistência. Isso é o registro, sem resultado prometido.";
    }

    private static string Consistency(AssistantUserContext? context)
    {
        if (context is { ActiveHabits: 0 }) return "Crie um hábito pequeno antes de medir consistência. Um horário fixo ajuda mais do que uma lista longa.";
        return "Se a semana falhou, reduza a lista e mantenha o horário. Escolha um hábito para repetir amanhã. Consistência é repetição, não resultado prometido.";
    }

    private static string? ResolveGuide(string message)
    {
        var text = Fold(message);
        if (Has(text, "suporte") || Has(text, "chamado")) return "suporte";
        if (Has(text, "comercial") || Has(text, "mnsoft")) return "comercial";
        if (Has(text, "cancelar")) return "cancelar";
        if (Has(text, "privacidade") || Has(text, "lgpd")) return "privacidade";
        if (Has(text, "limite") || Has(text, "cota") || Has(text, "teto")) return "limites";
        if (Has(text, "plano") || Has(text, "premium") || Has(text, "enterprise")) return "planos";
        if ((Has(text, "criar") || Has(text, "novo") || Has(text, "orient")) && Has(text, "habit")) return "habito";
        if (Has(text, "meta")) return "meta";
        if (Has(text, "habit")) return "habito";
        if (Has(text, "rotina") || Has(text, "meu dia")) return "rotina";
        if (Has(text, "evolu") || Has(text, "progresso") || Has(text, "relatorio") || Has(text, "interpret")) return "evolucao";
        if (Has(text, "consistenc") || Has(text, "melhorar")) return "consistencia";
        if (Has(text, "o que e") || Has(text, "habitflow")) return "sobre";
        return null;
    }

    private static bool Has(string text, string term) => text.Contains(term, StringComparison.Ordinal);

    private static string Fold(string value)
    {
        var lower = (value ?? "").ToLowerInvariant();
        return lower
            .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("õ", "o").Replace("ô", "o")
            .Replace("ú", "u")
            .Replace("ç", "c");
    }
}

public sealed class AssistantContextBuilder(IHabitRepository habits, IUserRepository users, IOptions<AssistantOptions> settings)
{
    public async Task<AssistantUserContext> BuildAsync(Guid clientId, Guid userId, CancellationToken ct)
    {
        var options = settings.Value;
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return new(0, 0, UserPlan.Free, 0);
        if (!options.AllowHabitContext) return new(0, 0, options.AllowBillingContext ? user.Plan : UserPlan.Free, 0);
        var list = await habits.ListAsync(clientId, userId, ct);
        return new(list.Count(x => !x.IsArchived && !x.IsPaused), list.Count(x => !x.IsArchived && x.IsPaused), options.AllowBillingContext ? user.Plan : UserPlan.Free, list.Count(x => x.ReminderTime.HasValue && !x.IsArchived));
    }
}

public sealed class DisabledAssistantProvider : IAssistantProvider
{
    public bool IsConfigured => false;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => Task.FromResult(new AssistantResponse("Assistente desabilitado.", "Disabled", "Disabled"));
}

public sealed class ConfiguredAssistantProvider(
    DisabledAssistantProvider disabled,
    DeterministicAssistantProvider knowledge,
    GroqAssistantProvider groq,
    GeminiAssistantProvider gemini,
    DeepSeekAssistantProvider deepSeek,
    IOptions<AssistantOptions> options) : IAssistantProvider
{
    private IAssistantProvider Current => options.Value.Provider switch
    {
        var p when p.Equals("Knowledge", StringComparison.OrdinalIgnoreCase) => knowledge,
        var p when p.Equals("Groq", StringComparison.OrdinalIgnoreCase) => groq,
        var p when p.Equals("Gemini", StringComparison.OrdinalIgnoreCase) => gemini,
        var p when p.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase) => deepSeek,
        _ => disabled
    };

    public bool IsConfigured => Current.IsConfigured;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => Current.GenerateAsync(request, context, ct);
}

public sealed class DeterministicAssistantProvider(AssistantKnowledgeService knowledge) : IAssistantProvider
{
    public bool IsConfigured => true;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var guided = knowledge.Guide(request.Message, context);
        if (guided is not null) return Task.FromResult(guided);
        var article = knowledge.Match(request.Message);
        var answer = article?.Answer ?? "Não encontrei essa informação na base do HabitFlow. Posso direcionar você ao suporte.";
        return Task.FromResult(new AssistantResponse(answer, "Knowledge", "Allowed", article is null ? "/support/tickets/new" : null, article is null ? "Abrir chamado" : null));
    }
}

public sealed class AssistantAuditService(ILogger<AssistantAuditService> logger)
{
    public void Write(EventId eventId, string code, AssistantRequest request, string status, string provider, long durationMs) =>
        logger.LogInformation(eventId, "{Code} CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} Status={Status} Provider={Provider} DurationMs={DurationMs}", code, request.CorrelationId, request.ClientId, request.UserId, status, provider, durationMs);
}

public sealed class AssistantConversationRepository(IAssistanceRepository inner)
{
    public Task<Guid> OpenAsync(Guid clientId, Guid userId, CancellationToken ct) => inner.GetOrCreateConversationAsync(clientId, userId, ct);
    public Task AddAsync(AssistantMessage message, CancellationToken ct) => inner.AddMessageAsync(message, ct);
    public Task DeleteAsync(Guid clientId, Guid userId, CancellationToken ct) => inner.DeleteHistoryAsync(clientId, userId, ct);
}

public sealed class AssistantChatService(AssistantConversationRepository conversations, AssistantContextBuilder contextBuilder, PlanEntitlementService entitlements, IAssistantProvider provider, AssistantSafetyService safety, AssistantAuditService audit, AiKnowledgeBaseService screens, AiChatService aiChat, AiAdminService admin, IOptions<AssistantOptions> settings, ILogger<AssistantChatService> logger)
{
    public bool IsEnabled => settings.Value.Enabled && provider.IsConfigured;
    public AssistantOptions Configuration => settings.Value;

    private static bool RequiresAiEntitlement(string provider) =>
        provider.Equals("Groq", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase);
    public Task<AssistantResponse> AskAsync(Guid clientId, Guid userId, string message, string correlationId, CancellationToken ct) =>
        AskAsync(clientId, userId, message, correlationId, null, ct);

    public async Task<AssistantResponse> AskAsync(Guid clientId, Guid userId, string message, string correlationId, string? screen, CancellationToken ct)
    {
        var options = settings.Value;
        var request = new AssistantRequest(message, clientId, userId, correlationId);
        var watch = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(message) || message.Length > Math.Clamp(options.MaxInputChars, 100, 10000)) return new("Revise sua mensagem e respeite o limite de caracteres.", "safety", "Invalid");
        var blocked = safety.InspectInput(message);
        if (blocked is not null)
        {
            var securityEvent = safety.SecurityEvent(message);
            if (securityEvent is not null)
                await admin.RecordAsync(clientId, userId, blocked.Provider, "", blocked.SafetyStatus, securityEvent, correlationId, watch.ElapsedMilliseconds, ct);
            audit.Write(ApplicationEvents.AiRequestBlockedByGuardrail, "ai.request.blocked_by_guardrail", request, blocked.SafetyStatus, blocked.Provider, watch.ElapsedMilliseconds);
            await admin.RecordAsync(clientId, userId, blocked.Provider, "", blocked.SafetyStatus, "ai.request.blocked_by_guardrail", correlationId, watch.ElapsedMilliseconds, ct);
            return blocked;
        }
        var screenHelp = screens.Explain(screen, message);
        if (screenHelp is not null)
        {
            audit.Write(ApplicationEvents.AssistantResponseGenerated, "assistant.screen.explained", request, screenHelp.SafetyStatus, screenHelp.Provider, watch.ElapsedMilliseconds);
            return screenHelp;
        }
        if (screens.MatchesGuide(message))
        {
            AssistantUserContext? guideContext = null;
            if (screens.NeedsAccount(message))
            {
                try
                {
                    using var guideTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    guideTimeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 120)));
                    guideContext = await contextBuilder.BuildAsync(clientId, userId, guideTimeout.Token);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    logger.LogWarning("assistant.guide.context_failed ErrorType={ErrorType}", ex.GetType().Name);
                }
            }
            var guided = screens.Answer(message, guideContext);
            if (guided is not null)
            {
                audit.Write(ApplicationEvents.AssistantResponseGenerated, "assistant.guide.answered", request, guided.SafetyStatus, guided.Provider, watch.ElapsedMilliseconds);
                return guided;
            }
        }
        if (!IsEnabled) { audit.Write(ApplicationEvents.AssistantDisabled, "assistant.disabled", request, "Disabled", options.Provider, 0); return new(options.DefaultMessage, "Disabled", "Disabled", "/support/tickets/new", "Falar com a MNSOFT"); }
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 120)));
            var context = await contextBuilder.BuildAsync(clientId, userId, timeout.Token);
            if (RequiresAiEntitlement(options.Provider) && !await entitlements.GetBooleanFeatureAsync(userId, PlanFeatureCodes.AiAssistant, timeout.Token))
            {
                var blockedByPlan = new AssistantResponse("Seu plano não inclui o Assistente IA. Consulte Planos para habilitar.", "safety", "BlockedByPlan", "/plans", "Ver planos");
                audit.Write(ApplicationEvents.AiRequestBlockedByPlan, "ai.request.blocked_by_plan", request, blockedByPlan.SafetyStatus, options.Provider, watch.ElapsedMilliseconds);
                await admin.RecordAsync(clientId, userId, options.Provider, "", blockedByPlan.SafetyStatus, "ai.request.blocked_by_plan", correlationId, watch.ElapsedMilliseconds, timeout.Token);
                return blockedByPlan;
            }
            audit.Write(ApplicationEvents.AssistantContextBuilt, "assistant.context.built", request, "Success", options.Provider, watch.ElapsedMilliseconds);
            var generated = RequiresAiEntitlement(options.Provider)
                ? await aiChat.CompleteAsync(request, context, timeout.Token)
                : await provider.GenerateAsync(request, context, timeout.Token);
            var response = safety.InspectOutput(generated, options.MaxOutputChars);
            if (safety.ContainsSensitiveData(generated.Message))
                await admin.RecordAsync(clientId, userId, response.Provider, "", "Blocked", "security.secret_exposure_prevented", correlationId, watch.ElapsedMilliseconds, timeout.Token);
            if (options.StoreConversationHistory)
            {
                var conversation = await conversations.OpenAsync(clientId, userId, timeout.Token);
                await conversations.AddAsync(new(Guid.NewGuid(), clientId, userId, conversation, "user", safety.Sanitize(message, options.MaxInputChars), safety.Sanitize(message, options.MaxInputChars), response.SafetyStatus, "local", DateTime.UtcNow, correlationId), timeout.Token);
                await conversations.AddAsync(new(Guid.NewGuid(), clientId, userId, conversation, "assistant", response.Message, response.Message, response.SafetyStatus, response.Provider, DateTime.UtcNow, correlationId), timeout.Token);
            }
            audit.Write(ApplicationEvents.AssistantResponseGenerated, "assistant.response.generated", request, response.SafetyStatus, response.Provider, watch.ElapsedMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            audit.Write(ApplicationEvents.AiProviderTimeout, "ai.provider.timeout", request, "Timeout", options.Provider, watch.ElapsedMilliseconds);
            await admin.RecordAsync(clientId, userId, options.Provider, "", "Timeout", "ai.provider.timeout", correlationId, watch.ElapsedMilliseconds, CancellationToken.None);
            return new("A resposta demorou mais que o esperado. Tente novamente ou fale com o suporte.", options.Provider, "Timeout", "/support/tickets/new", "Falar com suporte");
        }
        catch (Exception ex)
        {
            logger.LogError(ApplicationEvents.AiRequestFailed, "ai.request.failed CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} ErrorType={ErrorType}", correlationId, clientId, userId, ex.GetType().Name);
            await admin.RecordAsync(clientId, userId, options.Provider, "", "Error", "ai.request.failed", correlationId, watch.ElapsedMilliseconds, CancellationToken.None);
            return new("Não foi possível responder agora. Tente novamente ou fale com o suporte.", options.Provider, "Error", "/support/tickets/new", "Falar com suporte");
        }
    }
    public Task DeleteAsync(Guid clientId, Guid userId, CancellationToken ct) => conversations.DeleteAsync(clientId, userId, ct);
}

// Compatibility wrapper while controllers and integrations migrate to AssistantChatService.
public sealed class AssistantConversationService(AssistantChatService inner)
{
    public Task<AssistantResponse> AskAsync(Guid clientId, Guid userId, string message, string correlationId, CancellationToken ct) => inner.AskAsync(clientId, userId, message, correlationId, ct);
    public Task DeleteAsync(Guid clientId, Guid userId, CancellationToken ct) => inner.DeleteAsync(clientId, userId, ct);
}
