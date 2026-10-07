using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

/// <summary>
/// v6.19.7 — auditoria visual SaaS, contraste de campos, menus sobre formulários
/// e IA multi-provedor Groq/Gemini/DeepSeek com modelo padrão e status de erro.
/// </summary>
public sealed class VisualSaaSV6197Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root, Path.Combine(parts)));

    [Fact]
    public void Version_is_current_release()
    {
        Assert.Contains("<Version>6.19.9</Version>", Read("src", "HabitFlow.Web", "HabitFlow.Web.csproj"));
        Assert.Contains("\"Version\": \"v6.19.9\"", Read("src", "HabitFlow.Web", "appsettings.json"));
    }

    [Fact]
    public void Layout_loads_contrast_layer_after_design_system()
    {
        var layout = Read("src", "HabitFlow.Web", "Views", "Shared", "_Layout.cshtml");
        var design = layout.IndexOf("design-system-v2.css", StringComparison.Ordinal);
        var visual = layout.IndexOf("visual-saas-v6197.css", StringComparison.Ordinal);
        Assert.True(design >= 0 && visual > design, "a camada visual precisa vir depois do design system");
    }

    [Fact]
    public void Contrast_css_pairs_light_fields_and_keeps_ops_dark_fields_readable()
    {
        var css = Read("src", "HabitFlow.Web", "wwwroot", "css", "visual-saas-v6197.css");
        Assert.Contains("var(--hf-color-text)", css);
        var tokens = Read("src", "HabitFlow.Web", "wwwroot", "css", "design-system-v2.css");
        Assert.Contains("--hf-color-border-strong", tokens);
        Assert.Contains("--hf-color-danger", tokens);
        Assert.Contains("--hf-color-success", tokens);
        Assert.Contains("--hf-color-warning", tokens);
        Assert.Contains("h1,h2,h3{color:inherit", tokens);
        Assert.Contains("background-color: #ffffff", css);
        Assert.Contains("color: #eaf2ff", css);
        Assert.Contains("background-color: #0b1624", css);
        Assert.Contains(".assist-hero", css);
        Assert.Contains("color: #f8fafc", css);
        Assert.Contains("-webkit-text-fill-color: #17251f", css);
    }

    [Fact]
    public void Menus_stack_above_forms_and_escape_header_backdrop()
    {
        var css = Read("src", "HabitFlow.Web", "wwwroot", "css", "visual-saas-v6197.css");
        Assert.Contains("z-index: 1080", css);
        Assert.Contains("backdrop-filter: none", css);
        Assert.Contains(".hf-form-actions { z-index: 20; }", css);

        var script = Read("src", "HabitFlow.Web", "wwwroot", "js", "design-system-v2.js");
        Assert.Contains("strategy: 'fixed'", script);
    }

    [Fact]
    public void Providers_resolve_their_own_default_models()
    {
        Assert.Equal("llama-3.3-70b-versatile", AssistantModelCatalog.Resolve("", "", AssistantModelCatalog.GroqDefault));
        Assert.Equal("gemini-2.0-flash", AssistantModelCatalog.Resolve("  ", null, AssistantModelCatalog.GeminiDefault));
        Assert.Equal("deepseek-chat", AssistantModelCatalog.Resolve(null, "   ", AssistantModelCatalog.DeepSeekDefault));
        Assert.Equal("modelo-do-ambiente", AssistantModelCatalog.Resolve("", "modelo-do-ambiente", AssistantModelCatalog.GroqDefault));
        Assert.Equal("modelo-do-provedor", AssistantModelCatalog.Resolve("modelo-do-provedor", "modelo-do-ambiente", AssistantModelCatalog.GroqDefault));
    }

    [Fact]
    public void Provider_errors_are_not_marked_allowed_and_gemini_key_stays_out_of_the_url()
    {
        var failed = AssistantModelCatalog.FromOpenAiJson("Groq", false, "{\"error\":\"nope\"}");
        Assert.Equal("Error", failed.SafetyStatus);
        Assert.DoesNotContain("nope", failed.Message);

        var invalid = AssistantModelCatalog.FromGeminiJson(true, "{\"not\":\"a candidate\"}");
        Assert.Equal("Error", invalid.SafetyStatus);

        var ok = AssistantModelCatalog.FromOpenAiJson("DeepSeek", true, "{\"choices\":[{\"message\":{\"content\":\"Olá\"}}]}");
        Assert.Equal("Allowed", ok.SafetyStatus);
        Assert.Equal("Olá", ok.Message);

        var gemini = AssistantModelCatalog.FromGeminiJson(true, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Tudo bem\"}]}}]}");
        Assert.Equal("Allowed", gemini.SafetyStatus);
        Assert.Equal("Tudo bem", gemini.Message);

        var source = Read("src", "HabitFlow.Application", "Services", "AiAssistantProviders.cs");
        Assert.Contains("x-goog-api-key", source);
        Assert.DoesNotContain("?key=", source);
        Assert.Contains("AssistantModelCatalog.GroqDefault", source);
        Assert.Contains("AssistantModelCatalog.DeepSeekDefault", source);
        Assert.Contains("AssistantModelCatalog.GeminiDefault", source);
    }

    [Fact]
    public void Admin_screen_names_the_three_providers_without_asking_for_secrets()
    {
        var view = Read("src", "HabitFlow.Web", "Views", "AdminAssistant", "Index.cshtml");
        Assert.Contains("value=\"Groq\"", view);
        Assert.Contains("value=\"Gemini\"", view);
        Assert.Contains("value=\"DeepSeek\"", view);
        Assert.Contains("llama-3.3-70b-versatile", view);
        Assert.Contains("gemini-2.0-flash", view);
        Assert.Contains("deepseek-chat", view);
        Assert.DoesNotContain("ApiKey", view);
    }
}
