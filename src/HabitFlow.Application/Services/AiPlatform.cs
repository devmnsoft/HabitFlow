using System.Collections.Concurrent;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public bool Enabled { get; set; }
    public string DefaultProvider { get; set; } = "";
    public string DefaultModel { get; set; } = "";
    public int MaxTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.2;
    public AiProviderSet Providers { get; set; } = new();
}

public sealed class AiProviderSet
{
    public AiProviderEndpoint Groq { get; set; } = new() { BaseUrl = "https://api.groq.com/openai/v1" };
    public AiProviderEndpoint Gemini { get; set; } = new() { BaseUrl = "https://generativelanguage.googleapis.com/v1beta" };
    public AiProviderEndpoint DeepSeek { get; set; } = new() { BaseUrl = "https://api.deepseek.com" };
}

public sealed class AiProviderEndpoint
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
    public string[] AllowedModels { get; set; } = [];
}

public static class AiSecretResolver
{
    public const string GroqVariable = "HABITFLOW_AI_GROQ_API_KEY";
    public const string GeminiVariable = "HABITFLOW_AI_GEMINI_API_KEY";
    public const string DeepSeekVariable = "HABITFLOW_AI_DEEPSEEK_API_KEY";

    public static string Read(string variable) => Environment.GetEnvironmentVariable(variable)?.Trim() ?? "";

    public static string ForProvider(string provider) => provider.ToLowerInvariant() switch
    {
        "groq" => Read(GroqVariable),
        "gemini" => Read(GeminiVariable),
        "deepseek" => Read(DeepSeekVariable),
        _ => ""
    };
}

public static class AiModelPolicy
{
    public static bool IsAllowed(string? model, IEnumerable<string>? allowed, params string?[] defaults)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        var list = (allowed ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        if (list.Length > 0) return list.Any(item => item.Equals(model.Trim(), StringComparison.OrdinalIgnoreCase));
        return defaults.Any(item => !string.IsNullOrWhiteSpace(item) && item.Equals(model.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public static class AiGenerationSettings
{
    public static double Temperature(AiOptions options) => Math.Clamp(options.Temperature, 0, 2);
    public static int MaxTokens(AiOptions options) => Math.Clamp(options.MaxTokens <= 0 ? 800 : options.MaxTokens, 1, 8192);
}

public static class AiLogSanitizer
{
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var clean = System.Text.RegularExpressions.Regex.Replace(value, @"(?i)(bearer\s+)[A-Za-z0-9._~-]+", "$1[REMOVIDO]");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"(?i)(password|senha|api[_-]?key|token|secret)\s*[:=]\s*\S+", "$1=[REMOVIDO]");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\bsk-[A-Za-z0-9]{8,}\b", "[REMOVIDO]");
        return clean.Length <= 300 ? clean : clean[..300];
    }
}

public interface IAiProvider
{
    string Name { get; }
    bool IsEnabled { get; }
    string ResolveModel();
    bool IsModelAllowed(string model);
    Task<AssistantResponse> CompleteAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct);
}

public interface IAiProviderFactory
{
    IAiProvider? Resolve(string? providerName);
}

public sealed class AiPromptGuardrailService(AssistantSafetyService safety)
{
    public AssistantResponse? Inspect(string message) => safety.InspectInput(message);
    public AssistantResponse Protect(AssistantResponse response, int maxChars) => safety.InspectOutput(response, maxChars);
    public string Sanitize(string value) => safety.Sanitize(value);
}

public sealed class AiUsageLimiter
{
    private readonly ConcurrentDictionary<string, int> counts = new();

    public int LimitFor(UserPlan plan) => plan == UserPlan.Premium ? 40 : 5;

    public bool TryConsume(Guid clientId, Guid userId, UserPlan plan)
    {
        var limit = LimitFor(plan);
        var key = $"{DateTime.UtcNow:yyyyMMdd}:{clientId:N}:{userId:N}";
        while (true)
        {
            var current = counts.GetOrAdd(key, 0);
            if (current >= limit) return false;
            if (counts.TryUpdate(key, current + 1, current)) return true;
        }
    }
}

public sealed class AiAuditService(ILogger<AiAuditService> logger)
{
    public void Write(Guid clientId, Guid userId, string provider, string model, string status, long durationMs) =>
        logger.LogInformation("ai.usage ClientId={ClientId} UserId={UserId} Provider={Provider} Model={Model} Status={Status} DurationMs={DurationMs}", clientId, userId, provider, model, status, durationMs);
}

public sealed class AiKnowledgeBaseService(AssistantKnowledgeService articles)
{
    private static readonly Dictionary<string, string> Screens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/dashboard"] = "No Painel você vê o resumo do dia, o próximo passo e alertas da conta. Use os cartões para abrir hábitos, metas ou relatórios.",
        ["/habits"] = "Em Hábitos, crie uma ação pequena, escolha a frequência e acompanhe o status. O botão Novo fica no topo.",
        ["/goals"] = "Em Metas, defina um alvo e um prazo. Você pode ligar hábitos existentes a essa meta.",
        ["/my-day"] = "Meu Dia mostra a rotina de hoje. Marque o que concluiu ou adie sem apagar o hábito.",
        ["/reminders"] = "Em Lembretes, escolha o hábito e o horário. Push depende da permissão do navegador.",
        ["/reports"] = "Relatórios mostram consistência no período permitido pelo seu plano.",
        ["/plans"] = "Planos compara Free, Premium, Team e Enterprise. A contratação usa o fluxo de checkout, não o chat.",
        ["/billing"] = "Minha assinatura mostra o plano atual e o estado da cobrança. Alterações de pagamento ficam nesta tela.",
        ["/support"] = "Suporte reúne dúvidas e abertura de chamado. Não envie senha nem token.",
        ["/assistant"] = "O Coach explica o HabitFlow com dados agregados. Ele não altera plano, senha ou dados de outro tenant.",
        ["/admin"] = "A área administrativa é restrita ao papel Admin e não mostra segredos de integração.",
        ["/superadmin"] = "O SuperAdmin opera a plataforma. Cada ação crítica pede confirmação e fica auditada.",
        ["/admin/onboarding"] = "A implantação mostra o que já foi feito, o que está pendente e o que pode ser ignorado com justificativa. A conclusão só libera quando cada etapa está encerrada.",
        ["/admin/templates"] = "Templates globais são só leitura para o tenant. Templates próprios dependem do plano e nascem já ligados à conta, sem digitar ID.",
        ["/superadmin/customer-success"] = "O painel mostra indicadores lidos do banco. Onde a leitura falha, o texto é não disponível.",
        ["/notifications/alerts"] = "Os avisos entram no aplicativo só quando a condição e a preferência permitem. E-mail e WhatsApp não são enviados por esta tela.",
        ["/habit-library"] = "Escolha um template e confirme antes de duplicar um hábito. O plano Free respeita o limite de hábitos ativos."
    };

    public bool MatchesGuide(string message) => articles.MatchesGuide(message);
    public bool NeedsAccount(string message) => articles.NeedsAccount(message);
    public AssistantResponse? Answer(string message, AssistantUserContext? context) => articles.Guide(message, context);

    public AssistantResponse? Explain(string? screen, string message)
    {
        if (!AsksForScreen(message)) return null;
        var path = KnownScreen(Normalize(screen)) ?? MatchPath(message) ?? "/assistant";
        var text = Screens.TryGetValue(path, out var help)
            ? help
            : articles.Match(message)?.Answer ?? "Abra o menu da página para ver as ações disponíveis. Se a dúvida continuar, fale com o suporte.";
        return new AssistantResponse(text, "Knowledge", "Allowed", path, "Abrir tela");
    }

    private static bool AsksForScreen(string message) =>
        message.Contains("como usar", StringComparison.OrdinalIgnoreCase)
        || message.Contains("esta tela", StringComparison.OrdinalIgnoreCase)
        || message.Contains("tela atual", StringComparison.OrdinalIgnoreCase);

    private static string? MatchPath(string message)
    {
        foreach (var path in Screens.Keys.OrderByDescending(item => item.Length))
        {
            var token = path.Trim('/');
            if (System.Text.RegularExpressions.Regex.IsMatch(message, $@"(?i)(^|[^a-z]){System.Text.RegularExpressions.Regex.Escape(token)}([^a-z]|$)"))
                return path;
        }
        return null;
    }

    private static string? KnownScreen(string? path)
    {
        if (path is null) return null;
        return Screens.Keys
            .Where(key => path.Equals(key, StringComparison.OrdinalIgnoreCase) || path.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(key => key.Length)
            .FirstOrDefault();
    }

    private static string? Normalize(string? screen)
    {
        if (string.IsNullOrWhiteSpace(screen) || screen.Length > 200 || !screen.StartsWith('/') || screen.StartsWith("//")) return null;
        var path = screen.Split('?', '#')[0].TrimEnd('/');
        if (path.Contains("..", StringComparison.Ordinal) || path.Contains('\\') || path.Contains(':')) return null;
        return path.Length == 0 ? "/" : path;
    }
}

public sealed class GroqAiProvider(GroqAssistantProvider inner, IOptions<GroqOptions> options, IOptions<AiOptions> ai, IOptions<AssistantOptions> assistant) : IAiProvider
{
    public string Name => "Groq";
    public bool IsEnabled => ai.Value.Enabled && ai.Value.Providers.Groq.Enabled && !string.IsNullOrWhiteSpace(AiSecretResolver.ForProvider(Name));
    public string ResolveModel() => AssistantModelCatalog.Resolve(options.Value.Model, First(ai.Value.DefaultModel, assistant.Value.Model), AssistantModelCatalog.GroqDefault);
    public bool IsModelAllowed(string model) => AiModelPolicy.IsAllowed(model, ai.Value.Providers.Groq.AllowedModels, AssistantModelCatalog.GroqDefault, ai.Value.DefaultModel, assistant.Value.Model);
    public Task<AssistantResponse> CompleteAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => inner.GenerateAsync(request, context, ct);
    private static string First(string? preferred, string? fallback) => string.IsNullOrWhiteSpace(preferred) ? fallback ?? "" : preferred;
}

public sealed class GeminiAiProvider(GeminiAssistantProvider inner, IOptions<GeminiOptions> options, IOptions<AiOptions> ai, IOptions<AssistantOptions> assistant) : IAiProvider
{
    public string Name => "Gemini";
    public bool IsEnabled => ai.Value.Enabled && ai.Value.Providers.Gemini.Enabled && !string.IsNullOrWhiteSpace(AiSecretResolver.ForProvider(Name));
    public string ResolveModel() => AssistantModelCatalog.Resolve(options.Value.Model, First(ai.Value.DefaultModel, assistant.Value.Model), AssistantModelCatalog.GeminiDefault);
    public bool IsModelAllowed(string model) => AiModelPolicy.IsAllowed(model, ai.Value.Providers.Gemini.AllowedModels, AssistantModelCatalog.GeminiDefault, ai.Value.DefaultModel, assistant.Value.Model);
    public Task<AssistantResponse> CompleteAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => inner.GenerateAsync(request, context, ct);
    private static string First(string? preferred, string? fallback) => string.IsNullOrWhiteSpace(preferred) ? fallback ?? "" : preferred;
}

public sealed class DeepSeekAiProvider(DeepSeekAssistantProvider inner, IOptions<DeepSeekOptions> options, IOptions<AiOptions> ai, IOptions<AssistantOptions> assistant) : IAiProvider
{
    public string Name => "DeepSeek";
    public bool IsEnabled => ai.Value.Enabled && ai.Value.Providers.DeepSeek.Enabled && !string.IsNullOrWhiteSpace(AiSecretResolver.ForProvider(Name));
    public string ResolveModel() => AssistantModelCatalog.Resolve(options.Value.Model, First(ai.Value.DefaultModel, assistant.Value.Model), AssistantModelCatalog.DeepSeekDefault);
    public bool IsModelAllowed(string model) => AiModelPolicy.IsAllowed(model, ai.Value.Providers.DeepSeek.AllowedModels, AssistantModelCatalog.DeepSeekDefault, ai.Value.DefaultModel, assistant.Value.Model);
    public Task<AssistantResponse> CompleteAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct) => inner.GenerateAsync(request, context, ct);
    private static string First(string? preferred, string? fallback) => string.IsNullOrWhiteSpace(preferred) ? fallback ?? "" : preferred;
}

public sealed class AiProviderFactory(GroqAiProvider groq, GeminiAiProvider gemini, DeepSeekAiProvider deepSeek) : IAiProviderFactory
{
    public IAiProvider? Resolve(string? providerName) => providerName?.Trim().ToLowerInvariant() switch
    {
        "groq" => groq,
        "gemini" => gemini,
        "deepseek" => deepSeek,
        _ => null
    };
}

public sealed class AiChatService(IAiProviderFactory factory, AiAdminService admin, AiAuditService audit, IOptions<AssistantOptions> assistant, IOptions<AiOptions> aiOptions, ILogger<AiChatService> logger)
{
    public async Task<AssistantResponse> CompleteAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        AiRuntimeSettings runtime;
        try { runtime = await admin.SettingsAsync(ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("ai.settings.unavailable ErrorType={ErrorType}", ex.GetType().Name);
            runtime = new(false, "", false, false, false, "", "", "", 0, DateTime.UtcNow);
        }
        var configured = string.IsNullOrWhiteSpace(assistant.Value.Provider) || assistant.Value.Provider.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
            ? runtime.DefaultProvider : assistant.Value.Provider;
        var provider = factory.Resolve(string.IsNullOrWhiteSpace(configured) ? null : configured);
        if (!aiOptions.Value.Enabled || !runtime.Enabled || provider is null || !provider.IsEnabled || !admin.ProviderEnabled(runtime, provider.Name))
        {
            await admin.RecordAsync(request.ClientId, request.UserId, provider is null ? configured : provider.Name, "", "Disabled", "ai.provider.unavailable", request.CorrelationId, 0, ct);
            return new AssistantResponse(AssistantModelCatalog.Unavailable, configured, "Disabled", "/support/tickets/new", "Falar com suporte");
        }

        var model = provider.ResolveModel();
        if (!provider.IsModelAllowed(model) || !admin.ModelListed(runtime, provider.Name, model))
            return new AssistantResponse("O modelo configurado não está na lista permitida deste provedor.", provider.Name, "Error");

        if (!await admin.AllowAsync(request.ClientId, request.UserId, context.Plan, ct))
        {
            await admin.RecordAsync(request.ClientId, request.UserId, provider.Name, model, "BlockedByPlan", "ai.request.blocked_by_plan", request.CorrelationId, 0, ct);
            return new AssistantResponse("Você atingiu o limite diário do Assistente IA para o seu plano.", provider.Name, "BlockedByPlan", "/plans", "Ver planos");
        }
        await admin.RecordAsync(request.ClientId, request.UserId, provider.Name, model, "Started", "ai.request.started", request.CorrelationId, 0, ct);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await provider.CompleteAsync(request, context, ct);
            audit.Write(request.ClientId, request.UserId, provider.Name, model, response.SafetyStatus, watch.ElapsedMilliseconds);
            var code = response.SafetyStatus == "Allowed" ? "ai.request.completed" : "ai.request.failed";
            await admin.RecordAsync(request.ClientId, request.UserId, provider.Name, model, response.SafetyStatus, code, request.CorrelationId, watch.ElapsedMilliseconds, ct);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("ai.request.failed Provider={Provider} ErrorType={ErrorType} Detail={Detail} CorrelationId={CorrelationId} ClientId={ClientId}", provider.Name, ex.GetType().Name, AiLogSanitizer.Clean(ex.Message), request.CorrelationId, request.ClientId);
            audit.Write(request.ClientId, request.UserId, provider.Name, model, "Error", watch.ElapsedMilliseconds);
            await admin.RecordAsync(request.ClientId, request.UserId, provider.Name, model, "Error", "ai.request.failed", request.CorrelationId, watch.ElapsedMilliseconds, CancellationToken.None);
            return new AssistantResponse(AssistantModelCatalog.Unavailable, provider.Name, "Error", "/support/tickets/new", "Falar com suporte");
        }
    }
}
