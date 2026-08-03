using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SaaSOperationsV60Tests
{
    [Fact]
    public void CustomerHealth_CalculatesHealthyScore()
    {
        var score = new CustomerHealthService().Calculate(Guid.NewGuid(), true, true, true, true, true, false, false, false, false);
        Assert.Equal(100, score.Score);
        Assert.Equal("Saudável", score.Status);
    }

    [Fact]
    public void CustomerHealth_OverdueAndBlockedReducesScoreToRisk()
    {
        var score = new CustomerHealthService().Calculate(Guid.NewGuid(), false, false, false, false, false, true, true, true, true);
        Assert.Equal("Risco", score.Status);
        Assert.Contains("Inadimplente", score.Signals);
    }

    [Theory]
    [InlineData("/privacy")]
    [InlineData("/terms")]
    [InlineData("/lgpd")]
    [InlineData("/admin/onboarding")]
    [InlineData("/admin/communications")]
    [InlineData("/superadmin/customer-success")]
    public void RequiredRoutes_AreDocumentedInSource(string route)
    {
        var root = RepositoryRootLocator.Root;
        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Concat(Directory.GetFiles(root, "*.cshtml", SearchOption.AllDirectories));
        Assert.Contains(files, file => File.ReadAllText(file).Contains(route, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DatabaseScript_UsesHabitflowSchemaForV60Tables()
    {
        var root = RepositoryRootLocator.Root;
        var sql = File.ReadAllText(Path.Combine(root, "database/migrations/028_client_onboarding.sql"));
        Assert.Contains("habitflow.client_onboarding", sql);
        Assert.Contains("habitflow.client_communications", sql);
        Assert.Contains("ux_client_communications_no_duplicate_billing", sql);
        Assert.DoesNotContain("create table public.", sql, StringComparison.OrdinalIgnoreCase);
    }
}
