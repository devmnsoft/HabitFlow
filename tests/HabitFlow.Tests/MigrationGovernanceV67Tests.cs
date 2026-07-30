using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class MigrationGovernanceV67Tests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    [Fact]
    public void Migration_versions_are_unique_ordered_and_dynamically_discoverable()
    {
        var files = Directory.GetFiles(Path.Combine(Root, "database", "migrations"), "*.sql")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && name.Length > 4 && name[3] == '_')
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var versions = files.Select(name => int.Parse(name![..3])).ToArray();

        Assert.Equal(versions.Length, versions.Distinct().Count());
        Assert.Equal(versions.Order(), versions);
        Assert.All(versions, version => Assert.InRange(version, 1, 999));
    }

    [Fact]
    public void Compatibility_hooks_are_versioned_checksum_guarded_and_not_hardcoded_in_runner()
    {
        var runner = File.ReadAllText(Path.Combine(Root, "scripts", "database", "run-migrations.sh"));
        var hook = File.ReadAllText(Path.Combine(Root, "scripts", "database", "compatibility-hooks", "035_system_settings_contract.sql"));

        Assert.Contains("hooks_dir", runner);
        Assert.Contains("run_hooks \"$version\"", runner);
        Assert.DoesNotContain("migration == 035", runner, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("schema_compatibility_fixes", hook);
        Assert.Contains("hook_checksum", hook);
        Assert.Contains("to_regclass('habitflow.system_settings')", hook);
    }

    [Fact]
    public void Canonical_runner_guards_checksum_and_uses_postgresql_advisory_lock()
    {
        var runner = File.ReadAllText(Path.Combine(Root, "scripts", "database", "run-migrations.sh"));
        Assert.Contains("pg_advisory_xact_lock", runner);
        Assert.Contains("Checksum divergence", runner);
        Assert.Contains("sha256sum", runner);
        Assert.Contains("app_version", runner);
    }

    [Fact]
    public void Status_service_does_not_contain_a_hardcoded_range()
    {
        var source = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Application", "Services", "SaaSOperationsServices.cs"));
        Assert.DoesNotContain("Enumerable.Range(1, 30)", source);
        Assert.Contains("EnumerateFiles", source);
    }

    [Fact]
    public void Web_publish_includes_the_real_migration_catalog()
    {
        var project = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Web", "HabitFlow.Web.csproj"));
        Assert.Contains("database/migrations/*.sql", project);
        Assert.Contains("CopyToPublishDirectory=\"PreserveNewest\"", project);
    }
}
