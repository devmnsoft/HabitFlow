using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ReleaseCandidateV6171Tests
{
    private static string Read(string path) => File.ReadAllText(RepositoryRootLocator.PathTo(path));

    [Fact]
    public void Feature_catalog_exposes_explicit_disabled_state()
    {
        Assert.True(Enum.IsDefined(PlanFeatureImplementationStatus.Disabled));
        var migration = Read("database/migrations/075_v6171_release_candidate_integrity.sql");
        Assert.Contains("implementation_status = 'Disabled'", migration);
        Assert.Contains("is_marketable and implementation_status <> 'Implemented'", migration);
    }

    [Fact]
    public void Aggregate_schema_includes_release_candidate_migration()
        => Assert.Contains("075_v6171_release_candidate_integrity.sql", Read("database/script_completo.sql"));

    [Fact]
    public void Browser_ci_creates_real_login_state_without_a_repository_secret()
    {
        var workflow = Read(".github/workflows/playwright-ci.yml");
        Assert.Contains("provision-ci-user.ps1", workflow);
        Assert.Contains("HABITFLOW_AUTH_OUTPUT", workflow);
        Assert.DoesNotContain("HABITFLOW_AUTH_STORAGE_B64", workflow);
        Assert.DoesNotContain("continue-on-error", workflow);
    }
}
