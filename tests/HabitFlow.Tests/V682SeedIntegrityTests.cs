using System.Text.RegularExpressions;

namespace HabitFlow.Tests;

using Xunit;
public sealed class V682SeedIntegrityTests
{
    private static readonly string Root = FindRoot();

    [Theory]
    [InlineData("database/seed_dev.sql")]
    [InlineData("database/seed_production_minimal.sql")]
    public void BillingRulesUseExplicitUuidCanonicalValuesAndUpsert(string relativePath)
    {
        var sql = File.ReadAllText(Path.Combine(Root, relativePath));
        Assert.Matches(new Regex(@"billing_communication_rules\s*\(\s*id\s*,", RegexOptions.IgnoreCase), sql);
        Assert.True(Regex.Matches(sql, @"gen_random_uuid\s*\(\s*\)", RegexOptions.IgnoreCase).Count >= 4);
        Assert.Contains("'due_minus_3'", sql);
        Assert.Contains("'due_today'", sql);
        Assert.Contains("'due_plus_2'", sql);
        Assert.Contains("'due_plus_5'", sql);
        Assert.Contains("'BeforeDueDate'", sql);
        Assert.Contains("'OnDueDate'", sql);
        Assert.Contains("'AfterDueDate'", sql);
        Assert.DoesNotContain("'overdue_plus_2'", sql);
        Assert.DoesNotContain("'overdue_plus_5'", sql);
        Assert.Matches(new Regex(@"on\s+conflict\s*\(\s*code\s*\)\s*do\s+update", RegexOptions.IgnoreCase), sql);
    }

    [Fact]
    public void ForwardMigrationAddsDefaultAndSafelyNormalizesLegacyRows()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database/migrations/049_billing_communication_rule_seed_integrity.sql"));
        Assert.Contains("ALTER COLUMN id SET DEFAULT gen_random_uuid()", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is_active = false", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BEGIN;", sql);
        Assert.Contains("COMMIT;", sql);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
