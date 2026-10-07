using Xunit;

namespace HabitFlow.Tests;

public sealed class ProductionUiV6200Tests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(RepositoryRootLocator.Root, Path.Combine(parts)));

    [Fact]
    public void VersionAndLayoutLoadProductionUiLayerLast()
    {
        var layout = Read("src", "HabitFlow.Web", "Views", "Shared", "_Layout.cshtml");

        Assert.Contains("<Version>6.20.0</Version>", Read("src", "HabitFlow.Web", "HabitFlow.Web.csproj"));
        Assert.Contains("\"Version\": \"v6.20.0\"", Read("src", "HabitFlow.Web", "appsettings.json"));
        Assert.Contains("production-ui-v6200.css", layout);
        Assert.True(
            layout.IndexOf("production-ui-v6200.css", StringComparison.Ordinal) >
            layout.IndexOf("product-activation-v6198.css", StringComparison.Ordinal),
            "A camada v6.20 precisa ser carregada depois das camadas visuais anteriores.");
    }

    [Fact]
    public void ProductionUiDefinesRequiredDesignTokens()
    {
        var css = Read("src", "HabitFlow.Web", "wwwroot", "css", "production-ui-v6200.css");

        foreach (var token in new[]
        {
            "--hf-prod-bg",
            "--hf-prod-surface",
            "--hf-prod-card",
            "--hf-prod-border",
            "--hf-prod-text",
            "--hf-prod-muted",
            "--hf-prod-disabled",
            "--hf-prod-focus",
            "--hf-prod-danger",
            "--hf-prod-success",
            "--hf-prod-warning",
            "--hf-prod-info",
            "--hf-prod-commercial",
            "--hf-prod-shadow-sm",
            "--hf-prod-radius",
            "--hf-prod-space-4",
            "--hf-prod-font",
            "--hf-prod-z-header",
            "--hf-prod-breakpoint-xs",
            "--hf-prod-breakpoint-xl"
        })
        {
            Assert.Contains(token, css);
        }
    }

    [Fact]
    public void ProductionUiHardensFormsOverlaysTablesAndMobile()
    {
        var css = Read("src", "HabitFlow.Web", "wwwroot", "css", "production-ui-v6200.css");

        Assert.Contains("min-height: 2.75rem", css);
        Assert.Contains("border: 1px solid var(--hf-prod-border-strong)", css);
        Assert.Contains(":focus-visible", css);
        Assert.Contains(".dropdown-menu", css);
        Assert.Contains("z-index: var(--hf-prod-z-menu)", css);
        Assert.Contains(".table-responsive", css);
        Assert.Contains("overflow-x: auto", css);
        Assert.Contains(".hf-empty-state", css);
        Assert.Contains("@media (max-width: 1023.98px)", css);
        Assert.Contains("@media (max-width: 767.98px)", css);
        Assert.Contains("@media (max-width: 374.98px)", css);
        Assert.DoesNotContain("display:none", css, StringComparison.OrdinalIgnoreCase);
    }
}
