using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SuperAdminBootstrapV6195Tests
{
    [Theory]
    [InlineData("18.160.057/0001-13", "18160057000113")]
    [InlineData("18160057000113", "18160057000113")]
    [InlineData(" COMERCIAL@MNSOFT.COM.BR ", "comercial@mnsoft.com.br")]
    public void Login_normalizes_email_and_document(string value, string expected) =>
        Assert.Equal(expected, AuthService.NormalizeLogin(value));

    [Fact]
    public void Migration_never_contains_an_initial_password()
    {
        var sql = File.ReadAllText(RepositoryRootLocator.PathTo("database/migrations/086_v6195_superadmin_bootstrap.sql"));
        Assert.DoesNotContain("password_hash", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on conflict", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Startup_requires_secret_and_keeps_global_policy_server_side()
    {
        var hosted = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Services/SuperAdminBootstrapHostedService.cs"));
        var controller = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Controllers/SuperAdminController.cs"));
        Assert.Contains("HABITFLOW_SUPERADMIN_INITIAL_PASSWORD", hosted);
        Assert.DoesNotContain("MNSoft@", hosted);
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", controller);
    }
}
