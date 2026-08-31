using Xunit;

namespace HabitFlow.Tests;

public sealed class SecurityLgpdV6190Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void User_export_is_tenant_scoped_and_excludes_auth_and_billing_secrets()
    {
        var migration = Read("database/migrations/082_v6190_security_lgpd_hardening.sql");
        Assert.Contains("u.id=p_user_id and u.client_id=p_client_id", migration);
        Assert.Contains("h.user_id=p_user_id and h.client_id=p_client_id", migration);
        Assert.DoesNotContain("password_hash", migration);
        Assert.DoesNotContain("payment_transactions", migration);
        Assert.DoesNotContain("user_sessions", migration);
    }

    [Fact]
    public void Export_route_is_authenticated_antiforgery_protected_and_audited()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AccountPrivacyController.cs");
        var service = Read("src/HabitFlow.Web/Services/AccountPrivacyService.cs");
        Assert.Contains("[Authorize]", controller);
        Assert.Contains("[HttpPost(\"export/json\"), ValidateAntiForgeryToken]", controller);
        Assert.Contains("data_export.requested", service);
        Assert.Contains("data_export.completed", service);
    }

    [Theory]
    [InlineData("user_consent_history")]
    [InlineData("data_exports")]
    [InlineData("account_deletion_requests")]
    [InlineData("security_audit_events")]
    public void Governance_tables_are_incremental_and_indexed(string table)
    {
        var migration = Read("database/migrations/082_v6190_security_lgpd_hardening.sql");
        Assert.Contains($"create table if not exists {table}", migration);
        Assert.Contains("client_id uuid not null", migration);
        Assert.Contains("create index if not exists", migration);
    }

    [Fact]
    public void Deletion_lifecycle_and_versioned_optional_consents_are_constrained()
    {
        var migration = Read("database/migrations/082_v6190_security_lgpd_hardening.sql");
        foreach (var status in new[] { "Requested", "Confirmed", "Processing", "Completed", "Canceled", "Failed" })
            Assert.Contains($"'{status}'", migration);
        foreach (var consent in new[] { "terms", "privacy", "analytics", "notifications", "assistant_context" })
            Assert.Contains($"'{consent}'", migration);
        Assert.Contains("document_version", migration);
        Assert.Contains("occurred_at timestamptz", migration);
    }

    [Fact]
    public void Admin_routes_remain_elevated_and_security_headers_and_rate_limits_are_configured()
    {
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", Read("src/HabitFlow.Web/Controllers/SuperAdminController.cs"));
        var auth = Read("src/HabitFlow.Web/Configuration/AuthenticationConfig.cs");
        Assert.Contains("Cookie.HttpOnly = true", auth);
        Assert.Contains("CookieSecurePolicy.Always", auth);
        Assert.Contains("SameSiteMode.Lax", auth);
        var web = Read("src/HabitFlow.Web/Configuration/DependencyInjection.cs");
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", web);
        Assert.Contains("AddFixedWindowLimiter(\"assistant\"", web);
    }
}
