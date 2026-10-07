using System.Net;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AiPlatformV6197Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root, Path.Combine(parts)));

    [Fact]
    public void Ai_configuration_is_off_and_keeps_keys_out_of_source()
    {
        var json = Read("src", "HabitFlow.Web", "appsettings.json");
        using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var ai = doc.RootElement.GetProperty("Ai");
        Assert.False(ai.GetProperty("Enabled").GetBoolean());
        Assert.Equal("", ai.GetProperty("DefaultProvider").GetString());
        Assert.Equal("", ai.GetProperty("DefaultModel").GetString());
        Assert.Equal(800, ai.GetProperty("MaxTokens").GetInt32());
        Assert.Equal(0.2, ai.GetProperty("Temperature").GetDouble());
        Assert.False(ai.GetProperty("Providers").GetProperty("Groq").GetProperty("Enabled").GetBoolean());
        Assert.Equal("https://api.groq.com/openai/v1", ai.GetProperty("Providers").GetProperty("Groq").GetProperty("BaseUrl").GetString());
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta", ai.GetProperty("Providers").GetProperty("Gemini").GetProperty("BaseUrl").GetString());
        Assert.Equal("https://api.deepseek.com", ai.GetProperty("Providers").GetProperty("DeepSeek").GetProperty("BaseUrl").GetString());
        Assert.Equal(0, ai.GetProperty("Providers").GetProperty("Gemini").GetProperty("AllowedModels").GetArrayLength());
        Assert.DoesNotContain("\"ApiKey\"", json);
        Assert.DoesNotContain("sk-", json);

        var source = Read("src", "HabitFlow.Application", "Services", "AiPlatform.cs");
        Assert.Contains("HABITFLOW_AI_GROQ_API_KEY", source);
        Assert.Contains("HABITFLOW_AI_GEMINI_API_KEY", source);
        Assert.Contains("HABITFLOW_AI_DEEPSEEK_API_KEY", source);
        Assert.Contains("IAiProvider", source);
        Assert.Contains("AiChatService", source);
        Assert.Contains("AiPromptGuardrailService", source);
        Assert.Contains("AiUsageLimiter", source);
        Assert.Contains("AiAuditService", source);
        Assert.Contains("AiKnowledgeBaseService", source);
    }

    [Fact]
    public void Model_policy_rejects_unlisted_models_and_logs_drop_secrets()
    {
        Assert.True(AiModelPolicy.IsAllowed("llama-3.3-70b-versatile", [], "llama-3.3-70b-versatile"));
        Assert.False(AiModelPolicy.IsAllowed("modelo-livre", [], "llama-3.3-70b-versatile"));
        Assert.True(AiModelPolicy.IsAllowed("custom", ["custom"]));
        Assert.False(AiModelPolicy.IsAllowed("custom", ["outro"]));
        Assert.Equal(800, AiGenerationSettings.MaxTokens(new AiOptions()));
        Assert.Equal(0.2, AiGenerationSettings.Temperature(new AiOptions()));

        var clean = AiLogSanitizer.Clean("Authorization: Bearer sk-abcdefghijklmnop api_key=supersegredo");
        Assert.DoesNotContain("sk-abc", clean);
        Assert.DoesNotContain("supersegredo", clean);
        Assert.Contains("[REMOVIDO]", clean);
    }

    [Fact]
    public void Usage_limit_follows_the_plan_and_stays_inside_the_tenant_user()
    {
        var limiter = new AiUsageLimiter();
        var client = Guid.NewGuid();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        Assert.Equal(5, limiter.LimitFor(UserPlan.Free));
        Assert.Equal(40, limiter.LimitFor(UserPlan.Premium));
        for (var i = 0; i < 5; i++) Assert.True(limiter.TryConsume(client, user, UserPlan.Free));
        Assert.False(limiter.TryConsume(client, user, UserPlan.Free));
        Assert.True(limiter.TryConsume(client, other, UserPlan.Free));
    }

    [Fact]
    public void Screen_help_uses_the_current_path_and_ignores_external_addresses()
    {
        var knowledge = new AiKnowledgeBaseService(new AssistantKnowledgeService());
        var habits = knowledge.Explain("/habits/new?from=menu", "como usar esta tela");
        Assert.NotNull(habits);
        Assert.Equal("/habits", habits!.ActionUrl);
        Assert.Contains("Hábitos", habits.Message);

        var superAdmin = knowledge.Explain(null, "como usar o superadmin");
        Assert.Equal("/superadmin", superAdmin!.ActionUrl);

        var external = knowledge.Explain("//evil.example/admin", "como usar esta tela");
        Assert.Equal("/assistant", external!.ActionUrl);
        Assert.Null(knowledge.Explain("/billing", "qual o clima hoje"));
    }

    [Fact]
    public async Task Disabled_provider_returns_a_friendly_fallback()
    {
        var admin = new AiAdminService(new DisabledAdminRepository(), new AiUsageLimiter(), NullLogger<AiAdminService>.Instance);
        var service = new AiChatService(new MissingProviderFactory(), admin, new AiAuditService(NullLogger<AiAuditService>.Instance), Options.Create(new AssistantOptions { Provider = "Groq" }), Options.Create(new AiOptions()), NullLogger<AiChatService>.Instance);
        var response = await service.CompleteAsync(new AssistantRequest("oi", Guid.NewGuid(), Guid.NewGuid(), "c"), new AssistantUserContext(0, 0, UserPlan.Free, 0), CancellationToken.None);
        Assert.Equal("Disabled", response.SafetyStatus);
        Assert.Contains("indisponível", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Providers_turn_rate_limit_into_a_friendly_status_without_leaking_the_key()
    {
        Environment.SetEnvironmentVariable(AiSecretResolver.GroqVariable, "groq-test-key");
        Environment.SetEnvironmentVariable(AiSecretResolver.GeminiVariable, "gemini-test-key");
        try
        {
            var groqHandler = new StatusHandler(HttpStatusCode.TooManyRequests);
            var groq = new GroqAssistantProvider(new HttpClient(groqHandler) { BaseAddress = new Uri("https://api.groq.com/openai/v1/") }, Options.Create(new GroqOptions()), Options.Create(new AssistantOptions()), Options.Create(new AiOptions()), NullLogger<GroqAssistantProvider>.Instance);
            var groqResponse = await groq.GenerateAsync(Request(), Context(), CancellationToken.None);
            Assert.Equal("RateLimited", groqResponse.SafetyStatus);
            Assert.Contains("muitas perguntas", groqResponse.Message);
            Assert.DoesNotContain("groq-test-key", groqHandler.Body);
            Assert.Contains("\"max_tokens\":800", groqHandler.Body);

            var geminiHandler = new StatusHandler(HttpStatusCode.TooManyRequests);
            var gemini = new GeminiAssistantProvider(new HttpClient(geminiHandler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") }, Options.Create(new GeminiOptions()), Options.Create(new AssistantOptions()), Options.Create(new AiOptions()), NullLogger<GeminiAssistantProvider>.Instance);
            var geminiResponse = await gemini.GenerateAsync(Request(), Context(), CancellationToken.None);
            Assert.Equal("RateLimited", geminiResponse.SafetyStatus);
            Assert.DoesNotContain("gemini-test-key", geminiHandler.Last!.RequestUri!.ToString());
            Assert.True(geminiHandler.Last.Headers.Contains("x-goog-api-key"));
            Assert.Contains("models/gemini-2.0-flash:generateContent", geminiHandler.Last.RequestUri.ToString());

            var gate = new GroqAiProvider(groq, Options.Create(new GroqOptions { Model = "modelo-livre" }), Options.Create(new AiOptions { Enabled = true, Providers = new AiProviderSet { Groq = new AiProviderEndpoint { Enabled = true } } }), Options.Create(new AssistantOptions()));
            Assert.True(gate.IsEnabled);
            Assert.False(gate.IsModelAllowed(gate.ResolveModel()));
            var off = new GroqAiProvider(groq, Options.Create(new GroqOptions()), Options.Create(new AiOptions { Enabled = false, Providers = new AiProviderSet { Groq = new AiProviderEndpoint { Enabled = true } } }), Options.Create(new AssistantOptions()));
            Assert.False(off.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AiSecretResolver.GroqVariable, null);
            Environment.SetEnvironmentVariable(AiSecretResolver.GeminiVariable, null);
        }
    }

    [Fact]
    public void Assistant_admin_form_preserves_validation_contract()
    {
        var view = Read("src", "HabitFlow.Web", "Views", "AdminAssistant", "Index.cshtml");
        var script = Read("src", "HabitFlow.Web", "wwwroot", "js", "form-validation.js");
        var admin = Read("src", "HabitFlow.Web", "Controllers", "AdminAssistantController.cs");
        var assistant = Read("src", "HabitFlow.Web", "wwwroot", "js", "assistant-v6169.js");
        Assert.Contains("required", view);
        Assert.Contains("pattern=\"[A-Za-z0-9._:-]*\"", view);
        Assert.Contains("data-loading-text=\"Salvando…\"", view);
        Assert.Contains("data-confirm=", view);
        Assert.Contains("TempData[\"Success\"]", view);
        Assert.DoesNotContain("name=\"id\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preencha este campo obrigatório.", script);
        Assert.Contains("O formato informado não é válido.", script);
        Assert.Contains("Salvando…", script);
        Assert.Contains("return View(\"Index\", posted)", admin);
        Assert.Contains("Preencha este campo obrigatório.", admin);
        Assert.Contains("body.set('screen',location.pathname)", assistant);
        Assert.Contains("data-hf-async", Read("src", "HabitFlow.Web", "Views", "Assistant", "Index.cshtml"));
        Assert.Contains("requestSubmit()", Read("src", "HabitFlow.Web", "wwwroot", "js", "site.js"));
    }

    private static AssistantRequest Request() => new("como usar", Guid.NewGuid(), Guid.NewGuid(), "c");
    private static AssistantUserContext Context() => new(1, 0, UserPlan.Free, 0);

    private sealed class MissingProviderFactory : IAiProviderFactory
    {
        public IAiProvider? Resolve(string? providerName) => null;
    }

    private sealed class DisabledAdminRepository : IAiAdminRepository
    {
        public Task<AiRuntimeSettings> GetSettingsAsync(CancellationToken ct = default) => Task.FromResult(new AiRuntimeSettings(false, "", false, false, false, "", "", "", 200, DateTime.UtcNow));
        public Task SaveSettingsAsync(AiRuntimeSettings settings, Guid? userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AiPlanLimit>> ListPlanLimitsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiPlanLimit>>([]);
        public Task SavePlanLimitAsync(string planCode, int dailyLimit, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> GetEffectiveDailyLimitAsync(Guid clientId, CancellationToken ct = default) => Task.FromResult(5);
        public Task<int> CountUserTodayAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CountGlobalTodayAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task RecordAsync(Guid clientId, Guid userId, string provider, string model, string status, string eventCode, string correlationId, int durationMs, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AiUsageEventRow>> RecentAsync(Guid? clientId, int take, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiUsageEventRow>>([]);
        public Task<IReadOnlyList<AiUsageEventRow>> RecentFailuresAsync(Guid? clientId, int take, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiUsageEventRow>>([]);
        public Task<IReadOnlyList<AiTenantConsumption>> ConsumptionAsync(Guid? clientId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiTenantConsumption>>([]);
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("{\"error\":\"sk-should-not-leak\"}") };
        }
    }
}
