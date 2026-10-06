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
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}

public sealed class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
}

internal static class AssistantPromptBuilder
{
    public static string BuildSystemPrompt(AssistantUserContext context) =>
        $"Você é o assistente do HabitFlow. Responda em português do Brasil, com objetividade, sem inventar recursos. " +
        $"Não solicite segredos (senhas, tokens) e não inclua dados de outros usuários/tenants. " +
        $"Contexto autorizado (agregado): {context.ActiveHabits} hábitos ativos, {context.PausedHabits} pausados, {context.Reminders} lembretes, plano {context.Plan}.";
}

public abstract class OpenAiCompatibleAssistantProviderBase(HttpClient http, IOptions<AssistantOptions> assistantOptions, ILogger logger) : IAssistantProvider
{
    protected abstract string ProviderName { get; }
    protected abstract string ApiKey { get; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(assistantOptions.Value.Model);

    public async Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        if (!IsConfigured)
            return new AssistantResponse("Assistente não configurado.", ProviderName, "Disabled");

        var model = assistantOptions.Value.Model.Trim();
        var system = AssistantPromptBuilder.BuildSystemPrompt(context);

        var payload = new
        {
            model,
            temperature = 0.2,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = request.Message }
            }
        };

        var message = await PostAndExtractAsync(payload, ct);
        return new AssistantResponse(message, ProviderName, "Allowed");
    }

    protected virtual async Task<string> PostAndExtractAsync(object payload, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(httpRequest, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("assistant.provider.http_failed Provider={Provider} StatusCode={StatusCode}", ProviderName, (int)response.StatusCode);
            return "O assistente está indisponível no momento. Tente novamente ou fale com o suporte.";
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(content)
                ? "Não consegui gerar uma resposta agora. Tente novamente."
                : content!;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "assistant.provider.invalid_response Provider={Provider}", ProviderName);
            return "O assistente recebeu uma resposta inválida do provedor. Tente novamente.";
        }
    }
}

public sealed class GroqAssistantProvider(HttpClient http, IOptions<GroqOptions> options, IOptions<AssistantOptions> assistantOptions, ILogger<GroqAssistantProvider> logger)
    : OpenAiCompatibleAssistantProviderBase(http, assistantOptions, logger)
{
    protected override string ProviderName => "Groq";
    protected override string ApiKey => options.Value.ApiKey;
}

public sealed class DeepSeekAssistantProvider(HttpClient http, IOptions<DeepSeekOptions> options, IOptions<AssistantOptions> assistantOptions, ILogger<DeepSeekAssistantProvider> logger)
    : OpenAiCompatibleAssistantProviderBase(http, assistantOptions, logger)
{
    protected override string ProviderName => "DeepSeek";
    protected override string ApiKey => options.Value.ApiKey;
}

public sealed class GeminiAssistantProvider(HttpClient http, IOptions<GeminiOptions> options, IOptions<AssistantOptions> assistantOptions, ILogger<GeminiAssistantProvider> logger) : IAssistantProvider
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey) && !string.IsNullOrWhiteSpace(assistantOptions.Value.Model);

    public async Task<AssistantResponse> GenerateAsync(AssistantRequest request, AssistantUserContext context, CancellationToken ct)
    {
        if (!IsConfigured)
            return new AssistantResponse("Assistente não configurado.", "Gemini", "Disabled");

        var model = assistantOptions.Value.Model.Trim();
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
            }
        };

        var url = $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(options.Value.ApiKey)}";
        using var response = await http.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("assistant.provider.http_failed Provider=Gemini StatusCode={StatusCode}", (int)response.StatusCode);
            return new AssistantResponse("O assistente está indisponível no momento. Tente novamente ou fale com o suporte.", "Gemini", "Error");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return new AssistantResponse(string.IsNullOrWhiteSpace(text) ? "Não consegui gerar uma resposta agora." : text!, "Gemini", "Allowed");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "assistant.provider.invalid_response Provider=Gemini");
            return new AssistantResponse("O assistente recebeu uma resposta inválida do provedor. Tente novamente.", "Gemini", "Error");
        }
    }
}

