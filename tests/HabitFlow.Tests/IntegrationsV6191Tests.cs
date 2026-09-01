using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class IntegrationsV6191Tests
{
    [Fact]
    public void ApiSecretsAreHashedDeterministicallyWithoutRetainingPlaintext()
    {
        const string secret = "hf_live_not-a-real-secret";
        var hash = IntegrationService.HashSecret(secret);
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain(secret, hash, StringComparison.Ordinal);
        Assert.Equal(hash, IntegrationService.HashSecret(secret));
    }

    [Theory]
    [InlineData("habits.read")]
    [InlineData("habits.write")]
    [InlineData("goals.read")]
    [InlineData("goals.write")]
    [InlineData("routines.read")]
    [InlineData("checkins.write")]
    [InlineData("notifications.read")]
    [InlineData("profile.read")]
    public void RequiredScopesAreAllowlisted(string scope) => Assert.Contains(scope, IntegrationScopes.Allowed);

    [Fact]
    public void MigrationDefinesTenantIndexesIdempotencyAndHashedCredentials()
    {
        var root = FindRoot();
        var sql = File.ReadAllText(Path.Combine(root, "database/migrations/083_v6191_public_integrations.sql"));
        Assert.Contains("key_hash char(64)", sql);
        Assert.Contains("token_hash char(64)", sql);
        Assert.Contains("unique(webhook_id,event_id,attempt)", sql);
        Assert.Contains("client_id", sql);
        Assert.DoesNotContain("api_key text", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicApiRequiresAuthorizationScopeRateLimitAndTenant()
    {
        var root = FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "src/HabitFlow.Web/Controllers/PublicApiController.cs"));
        Assert.Contains("[ApiController, Authorize, EnableRateLimiting", source);
        Assert.Contains("User.HasClaim(\"scope\", \"habits.read\")", source);
        Assert.Contains("current.ClientId", source);
        Assert.Contains("ListAsync(clientId, current.UserId", source);
    }

    static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
