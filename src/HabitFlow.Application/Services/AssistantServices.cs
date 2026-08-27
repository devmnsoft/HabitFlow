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
    private static readonly string[] Bypass = ["burlar o plano", "contornar o limite", "mais hábitos sem pagar", "dados de outro"];

    public bool ContainsSensitiveData(string value) => SecretPattern.IsMatch(value ?? "");
    public bool IsPromptInjection(string value) => HasAny(value, Injection);
    public bool IsOutOfScope(string value) => HasAny(value, Medical) || HasAny(value, LegalFinancial);
    public bool IsDestructive(string value) => HasAny(value, Bypass);
    public AssistantResponse? InspectInput(string value)
    {
        if (ContainsSensitiveData(value) || IsPromptInjection(value) || HasAny(value, Bypass))
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
        new("O que é o HabitFlow", "sobre", "Começar", "O que é o HabitFlow?", "O HabitFlow ajuda a planejar hábitos, metas, lembretes e acompanhar sua evolução sem prometer resultados garantidos.", ["habitflow","começar"], true, 1, Updated),
        new("Criar hábito", "criar-habito", "Hábitos", "Como criar um hábito?", "Abra Hábitos, selecione Criar hábito e informe nome e frequência. Um começo pequeno costuma ser mais sustentável.", ["criar","hábito"], true, 2, Updated),
        new("Criar meta", "criar-meta", "Metas", "Como criar uma meta?", "Abra Metas e selecione Nova meta. Defina um alvo mensurável e um período realista.", ["criar","meta"], true, 3, Updated),
        new("Lembretes", "lembretes", "Notificações", "Como configurar lembretes?", "Abra Lembretes, escolha um hábito e um horário. Para push, permita notificações nas configurações do navegador e do HabitFlow.", ["lembrete","notificação"], true, 4, Updated),
        new("Progresso", "progresso", "Progresso", "Como acompanhar o progresso?", "Use Meu Dia para registrar conclusões e Relatórios para acompanhar consistência e evolução.", ["progresso","streak","relatório"], true, 5, Updated),
        new("Planos e limites", "planos", "Conta", "Qual a diferença entre planos?", "O plano Free oferece o acompanhamento essencial com limites. Consulte Planos para os limites atuais; o assistente não contorna limites nem presume benefícios não exibidos ali.", ["free","premium","limite","plano"], true, 6, Updated),
        new("Cancelar assinatura", "cancelar-assinatura", "Conta", "Como cancelar a assinatura?", "Abra Assinatura e use o fluxo de cancelamento disponível. Se a opção não aparecer, fale com o suporte; o chat não altera cobranças.", ["cancelar","assinatura"], true, 7, Updated),
        new("Privacidade", "privacidade", "Segurança", "Meus hábitos são privados?", "Seu contexto pessoal é isolado por empresa e usuário. O assistente usa apenas agregados autorizados e não inclui nomes ou notas de hábitos em contexto corporativo.", ["privacidade","lgpd","corporativo"], true, 8, Updated),
        new("Programas corporativos", "corporativo", "Corporativo", "Como funcionam programas corporativos?", "Programas corporativos mostram somente informações autorizadas e agregadas aos gestores. Hábitos privados não são expostos.", ["programa","corporativo","gestor"], true, 9, Updated),
        new("Suporte", "suporte", "Ajuda", "Como falar com suporte?", "Abra Suporte para consultar dúvidas comuns ou criar um chamado seguro com a MNSOFT. Nunca envie senhas ou tokens.", ["suporte","mnsoft"], true, 10, Updated),
        new("Boas práticas", "boas-praticas", "Hábitos", "Como melhorar minha rotina?", "Escolha poucas ações pequenas, associe-as a um horário viável e revise a semana sem buscar perfeição.", ["rotina","consistência","semana"], true, 11, Updated)
    ];
    public IReadOnlyList<HelpArticle> List(string? category = null) => Articles.Where(x => x.Active && (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.Order).ToArray();
    public HelpArticle? Get(string slug) => Articles.FirstOrDefault(x => x.Active && x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<HelpArticle> Search(string? query) => string.IsNullOrWhiteSpace(query) ? List() : Articles.Where(a => query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(t => ($"{a.Title} {a.Question} {a.Answer} {string.Join(' ', a.Tags)}").Contains(t, StringComparison.OrdinalIgnoreCase))).ToArray();
    public HelpArticle? Match(string message) => Articles.Select(a => new { Article = a, Score = a.Tags.Count(t => message.Contains(t, StringComparison.OrdinalIgnoreCase)) }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Article.Order).Select(x => x.Article).FirstOrDefault();
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

public sealed class ConfiguredAssistantProvider(DisabledAssistantProvider disabled, DeterministicAssistantProvider knowledge, IOptions<AssistantOptions> options) : IAssistantProvider
{
    private IAssistantProvider Current => options.Value.Provider.Equals("Knowledge", StringComparison.OrdinalIgnoreCase) ? knowledge : disabled;
    public bool IsConfigured => Current.IsConfigured;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => Current.GenerateAsync(request, context, ct);
}

public sealed class DeterministicAssistantProvider(AssistantKnowledgeService knowledge) : IAssistantProvider
{
    public bool IsConfigured => true;
    public Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var article = knowledge.Match(request.Message);
        var answer = article?.Answer ?? "Não encontrei essa informação na base do HabitFlow. Posso direcionar você ao suporte.";
        if (request.Message.Contains("meus hábitos", StringComparison.OrdinalIgnoreCase) || request.Message.Contains("atrasados", StringComparison.OrdinalIgnoreCase))
            answer = $"Você tem {context.ActiveHabits} hábitos ativos, {context.PausedHabits} pausados e {context.Reminders} lembretes. Por privacidade, não exponho nomes ou notas aqui. Seu plano é {context.Plan}.";
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

public sealed class AssistantChatService(AssistantConversationRepository conversations, AssistantContextBuilder contextBuilder, IAssistantProvider provider, AssistantSafetyService safety, AssistantAuditService audit, IOptions<AssistantOptions> settings, ILogger<AssistantChatService> logger)
{
    public bool IsEnabled => settings.Value.Enabled && provider.IsConfigured;
    public AssistantOptions Configuration => settings.Value;
    public async Task<AssistantResponse> AskAsync(Guid clientId, Guid userId, string message, string correlationId, CancellationToken ct)
    {
        var options = settings.Value;
        var request = new AssistantRequest(message, clientId, userId, correlationId);
        var watch = Stopwatch.StartNew();
        if (!IsEnabled) { audit.Write(ApplicationEvents.AssistantDisabled, "assistant.disabled", request, "Disabled", options.Provider, 0); return new(options.DefaultMessage, "Disabled", "Disabled", "/support/tickets/new", "Falar com a MNSOFT"); }
        if (string.IsNullOrWhiteSpace(message) || message.Length > Math.Clamp(options.MaxInputChars, 100, 10000)) return new("Revise sua mensagem e respeite o limite de caracteres.", "safety", "Invalid");
        var blocked = safety.InspectInput(message);
        if (blocked is not null) { audit.Write(ApplicationEvents.AssistantSafetyBlocked, safety.IsPromptInjection(message) ? "assistant.prompt_injection.detected" : "assistant.safety.blocked", request, blocked.SafetyStatus, blocked.Provider, watch.ElapsedMilliseconds); return blocked; }
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 120)));
            var context = await contextBuilder.BuildAsync(clientId, userId, timeout.Token);
            audit.Write(ApplicationEvents.AssistantContextBuilt, "assistant.context.built", request, "Success", options.Provider, watch.ElapsedMilliseconds);
            var response = safety.InspectOutput(await provider.GenerateAsync(request, context, timeout.Token), options.MaxOutputChars);
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
            audit.Write(ApplicationEvents.AssistantProviderTimeout, "assistant.provider.timeout", request, "Timeout", options.Provider, watch.ElapsedMilliseconds);
            return new("A resposta demorou mais que o esperado. Tente novamente ou fale com o suporte.", options.Provider, "Timeout", "/support/tickets/new", "Falar com suporte");
        }
        catch (Exception ex)
        {
            logger.LogError(ApplicationEvents.AssistantProviderError, "assistant.provider.error CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} ErrorType={ErrorType}", correlationId, clientId, userId, ex.GetType().Name);
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
