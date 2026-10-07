using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string Model { get; set; } = string.Empty;
}

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string Model { get; set; } = string.Empty;
}

public sealed class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";
    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    public string Model { get; set; } = string.Empty;
}

public static class AssistantModelCatalog
{
    public const string GroqDefault = "llama-3.3-70b-versatile";
    public const string GeminiDefault = "gemini-2.0-flash";
    public const string DeepSeekDefault = "deepseek-chat";
    public const string Unavailable = "O assistente está indisponível no momento. Tente novamente ou fale com o suporte.";
    public const string Invalid = "O assistente recebeu uma resposta inválida do provedor. Tente novamente.";
    public const string Empty = "Não consegui gerar uma resposta agora. Tente novamente.";

    public static string Resolve(string? providerModel, string? configuredModel, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(providerModel)) return providerModel.Trim();
        if (!string.IsNullOrWhiteSpace(configuredModel)) return configuredModel.Trim();
        return fallback;
    }

    public static AssistantResponse FromOpenAiJson(string provider, bool success, string? raw)
    {
        if (!success) return new AssistantResponse(Unavailable, provider, "Error");
        try
        {
            using var doc = JsonDocument.Parse(raw ?? "");
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return new AssistantResponse(string.IsNullOrWhiteSpace(content) ? Empty : content, provider, "Allowed");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return new AssistantResponse(Invalid, provider, "Error");
        }
    }

    public static AssistantResponse FromGeminiJson(bool success, string? raw)
    {
        if (!success) return new AssistantResponse(Unavailable, "Gemini", "Error");
        try
        {
            using var doc = JsonDocument.Parse(raw ?? "");
            var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return new AssistantResponse(string.IsNullOrWhiteSpace(text) ? Empty : text, "Gemini", "Allowed");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return new AssistantResponse(Invalid, "Gemini", "Error");
        }
    }
}

internal static class AssistantPromptBuilder
{
    public static string BuildSystemPrompt(AssistantUserContext context) =>
        $"Você é o assistente do HabitFlow. Responda em português do Brasil, com frases curtas, sem inventar preço, recurso ou diagnóstico. " +
        $"Não prometa resultado, não libere plano pago, não cancele assinatura e não peça chave, token ou senha. " +
        $"Não inclua dados de outros usuários ou tenants. " +
        $"Contexto autorizado (agregado): {context.ActiveHabits} hábitos ativos, {context.PausedHabits} pausados, {context.Reminders} lembretes, plano {context.Plan}.";
}

public abstract class OpenAiCompatibleAssistantProviderBase(HttpClient http, IOptions<AssistantOptions> assistantOptions, IOptions<AiOptions> aiOptions, ILogger logger) : IAssistantProvider
{
    protected abstract string ProviderName { get; }
    protected abstract string ApiKey { get; }
    protected abstract string ProviderModel { get; }
    protected abstract string FallbackModel { get; }

    protected string ResolvedModel => AssistantModelCatalog.Resolve(ProviderModel, string.IsNullOrWhiteSpace(aiOptions.Value.DefaultModel) ? assistantOptions.Value.Model : aiOptions.Value.DefaultModel, FallbackModel);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ResolvedModel);

    public async Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        if (!IsConfigured)
            return new AssistantResponse("Assistente não configurado.", ProviderName, "Disabled");

        var system = AssistantPromptBuilder.BuildSystemPrompt(context);
        var payload = new
        {
            model = ResolvedModel,
            temperature = AiGenerationSettings.Temperature(aiOptions.Value),
            max_tokens = AiGenerationSettings.MaxTokens(aiOptions.Value),
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = request.Message }
            }
        };

        return await PostAndExtractAsync(payload, ct);
    }

    protected virtual async Task<AssistantResponse> PostAndExtractAsync(object payload, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(httpRequest, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if ((int)response.StatusCode == 429)
        {
            logger.LogWarning("assistant.provider.rate_limited Provider={Provider}", ProviderName);
            return new AssistantResponse("O assistente recebeu muitas perguntas agora. Aguarde um instante e tente novamente.", ProviderName, "RateLimited");
        }
        if (!response.IsSuccessStatusCode)
            logger.LogWarning("assistant.provider.http_failed Provider={Provider} StatusCode={StatusCode}", ProviderName, (int)response.StatusCode);

        var parsed = AssistantModelCatalog.FromOpenAiJson(ProviderName, response.IsSuccessStatusCode, raw);
        if (parsed.SafetyStatus == "Error" && response.IsSuccessStatusCode)
            logger.LogWarning("assistant.provider.invalid_response Provider={Provider}", ProviderName);
        return parsed;
    }
}

public sealed class GroqAssistantProvider(HttpClient http, IOptions<GroqOptions> options, IOptions<AssistantOptions> assistantOptions, IOptions<AiOptions> aiOptions, ILogger<GroqAssistantProvider> logger)
    : OpenAiCompatibleAssistantProviderBase(http, assistantOptions, aiOptions, logger)
{
    protected override string ProviderName => "Groq";
    protected override string ApiKey => AiSecretResolver.ForProvider(ProviderName);
    protected override string ProviderModel => options.Value.Model;
    protected override string FallbackModel => AssistantModelCatalog.GroqDefault;
}

public sealed class DeepSeekAssistantProvider(HttpClient http, IOptions<DeepSeekOptions> options, IOptions<AssistantOptions> assistantOptions, IOptions<AiOptions> aiOptions, ILogger<DeepSeekAssistantProvider> logger)
    : OpenAiCompatibleAssistantProviderBase(http, assistantOptions, aiOptions, logger)
{
    protected override string ProviderName => "DeepSeek";
    protected override string ApiKey => AiSecretResolver.ForProvider(ProviderName);
    protected override string ProviderModel => options.Value.Model;
    protected override string FallbackModel => AssistantModelCatalog.DeepSeekDefault;
}

public sealed class GeminiAssistantProvider(HttpClient http, IOptions<GeminiOptions> options, IOptions<AssistantOptions> assistantOptions, IOptions<AiOptions> aiOptions, ILogger<GeminiAssistantProvider> logger) : IAssistantProvider
{
    private string ResolvedModel => AssistantModelCatalog.Resolve(options.Value.Model, string.IsNullOrWhiteSpace(aiOptions.Value.DefaultModel) ? assistantOptions.Value.Model : aiOptions.Value.DefaultModel, AssistantModelCatalog.GeminiDefault);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AiSecretResolver.ForProvider("Gemini")) && !string.IsNullOrWhiteSpace(ResolvedModel);

    public async Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        if (!IsConfigured)
            return new AssistantResponse("Assistente não configurado.", "Gemini", "Disabled");

        var system = AssistantPromptBuilder.BuildSystemPrompt(context);
        var prompt = system + "\n\nPergunta do usuário: " + request.Message;
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = AiGenerationSettings.Temperature(aiOptions.Value),
                maxOutputTokens = AiGenerationSettings.MaxTokens(aiOptions.Value)
            }
        };

        var url = $"models/{Uri.EscapeDataString(ResolvedModel)}:generateContent";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", AiSecretResolver.ForProvider("Gemini"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(httpRequest, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if ((int)response.StatusCode == 429)
        {
            logger.LogWarning("assistant.provider.rate_limited Provider=Gemini");
            return new AssistantResponse("O assistente recebeu muitas perguntas agora. Aguarde um instante e tente novamente.", "Gemini", "RateLimited");
        }
        if (!response.IsSuccessStatusCode)
            logger.LogWarning("assistant.provider.http_failed Provider=Gemini StatusCode={StatusCode}", (int)response.StatusCode);

        var parsed = AssistantModelCatalog.FromGeminiJson(response.IsSuccessStatusCode, raw);
        if (parsed.SafetyStatus == "Error" && response.IsSuccessStatusCode)
            logger.LogWarning("assistant.provider.invalid_response Provider=Gemini");
        return parsed;
    }
}

