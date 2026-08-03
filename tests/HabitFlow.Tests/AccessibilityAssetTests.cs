using Xunit;

namespace HabitFlow.Tests;

public sealed class AccessibilityAssetTests
{
    [Fact]
    public void Css_contains_required_accessibility_tokens()
    {
        var css = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/wwwroot/css/site.css"));
        foreach (var token in new[] { "--hf-bg", "--hf-surface", "--hf-text", "--hf-text-muted", "--hf-primary", "--mnsoft-blue", "body.hf-contrast-high", "body.hf-font-large", "body.hf-reduce-motion", ".mnsoft-official-logo" })
            Assert.Contains(token, css);
    }

    [Fact]
    public void Sql_scripts_contain_user_ui_preferences()
    {
        Assert.Contains("habitflow.user_ui_preferences", File.ReadAllText(RepositoryRootLocator.PathTo("database/script_completo.sql")));
        Assert.Contains("user_ui_preferences", File.ReadAllText(RepositoryRootLocator.PathTo("database/validate_schema_habitflow.sql")));
    }

    [Fact]
    public void Main_views_do_not_contain_placeholder_copy()
    {
        var files = Directory.GetFiles(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views"), "*.cshtml", SearchOption.AllDirectories);
        foreach (var file in files) Assert.DoesNotContain("Lorem ipsum", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
    }
}
