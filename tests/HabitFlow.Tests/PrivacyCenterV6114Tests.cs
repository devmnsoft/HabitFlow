using Xunit;

namespace HabitFlow.Tests;

public sealed class PrivacyCenterV6114Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Theory]
    [InlineData("[HttpGet(\"\")]")]
    [InlineData("[HttpPost(\"consents\"), ValidateAntiForgeryToken]")]
    [InlineData("[HttpPost(\"export-request\"), ValidateAntiForgeryToken]")]
    [InlineData("[HttpPost(\"delete-request\"), ValidateAntiForgeryToken]")]
    [InlineData("[HttpPost(\"anonymization-request\"), ValidateAntiForgeryToken]")]
    public void Privacy_routes_are_authenticated_and_antiforgery_protected(string contract)
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AccountPrivacyController.cs");
        Assert.Contains("[Authorize]", controller);
        Assert.Contains("[Route(\"account/privacy\")]", controller);
        Assert.Contains(contract, controller);
    }

    [Fact]
    public void Privacy_access_is_scoped_to_bound_identity_and_repository_queries_current_user()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AccountPrivacyController.cs");
        var repository = Read("src/HabitFlow.Infrastructure/Repositories/LgpdRepository.cs");
        Assert.Contains("CurrentUserId() != Guid.Empty && this.CurrentClientId() != Guid.Empty", controller);
        Assert.Contains("u.client_id=@clientId and r.user_id=@userId", repository);
        Assert.DoesNotContain("select * from habitflow.lgpd_requests", repository);
    }

    [Fact]
    public void Critical_requests_are_pending_audited_and_use_accessible_confirmation()
    {
        var migration = Read("database/migrations/063_account_privacy_center.sql");
        var panel = Read("src/HabitFlow.Web/Views/AccountPrivacy/Partials/_DataDeletionPanel.cshtml");
        var modal = Read("src/HabitFlow.Web/Views/Shared/Partials/_ConfirmationDialog.cshtml");
        Assert.Contains("privacy_request_events", migration);
        Assert.Contains("trg_audit_privacy_request", migration);
        Assert.Contains("Nenhum dado foi excluído", Read("src/HabitFlow.Web/Controllers/AccountPrivacyController.cs"));
        Assert.Contains("data-feedback-confirm", panel);
        Assert.Contains("aria-describedby", modal);
        Assert.Contains("Enviar solicitação", modal);
    }

    [Fact]
    public void Header_has_all_required_progressive_breakpoints_and_safe_area()
    {
        var css = Read("src/HabitFlow.Web/wwwroot/css/app-header-v2.css");
        foreach (var breakpoint in new[] { "min-width:1440px", "min-width:1280px", "min-width:1024px", "min-width:768px", "max-width:767px", "max-width:429px", "max-width:359px", "max-width:319px", "max-width:279px", "max-width:239px" }) Assert.Contains(breakpoint, css);
        Assert.Contains("env(safe-area-inset-bottom)", css);
        Assert.Contains("overflow-x:clip", css);
    }

    [Fact]
    public void Feedback_uses_safe_dom_and_returns_focus()
    {
        var script = Read("src/HabitFlow.Web/wwwroot/js/feedback-v5.js");
        Assert.DoesNotContain("innerHTML", script);
        Assert.DoesNotContain("confirm(", script);
        Assert.Contains("trigger?.focus()", script);
    }
}
