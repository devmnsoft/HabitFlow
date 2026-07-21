using Xunit;

namespace HabitFlow.Tests;

public sealed class DatabaseScriptTests
{
    private static readonly string Script = File.ReadAllText(Path.Combine("..", "..", "..", "..", "database", "script_completo.sql"));

    [Theory]
    [InlineData("habitflow.users")]
    [InlineData("habitflow.habits")]
    [InlineData("habitflow.habit_completions")]
    [InlineData("habitflow.support_tickets")]
    [InlineData("habitflow.support_messages")]
    [InlineData("habitflow.system_audit_logs")]
    [InlineData("habitflow.admin_audit_logs")]
    [InlineData("habitflow.system_settings")]
    [InlineData("habitflow.login_attempts")]
    [InlineData("habitflow.lgpd_requests")]
    [InlineData("habitflow.billing_events")]
    [InlineData("habitflow.deployment_events")]
    public void Complete_script_contains_required_tables(string table)
    {
        Assert.Contains($"create table if not exists {table}", Script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_script_is_self_contained_and_contains_key_constraints()
    {
        Assert.DoesNotContain("\\i", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("constraint uq_habitflow_habit_completions_habit_date unique", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ix_habitflow_system_audit_logs_created_at", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into habitflow.system_settings", Script, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class SchemaHardeningTests
{
    private static readonly string Root = Path.Combine("..", "..", "..", "..");

    [Fact]
    public void Complete_script_uses_habitflow_schema_and_not_public_tables()
    {
        var script = File.ReadAllText(Path.Combine(Root, "database", "script_completo.sql"));
        Assert.Contains("create schema if not exists habitflow", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("create table if not exists users", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("create table " + "public.", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_habitflow_users_role", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ix_habitflow_users_email", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Schema_validation_script_exists()
    {
        Assert.True(File.Exists(Path.Combine(Root, "database", "validate_schema_habitflow.sql")));
    }

    [Fact]
    public void DbNames_exposes_qualified_table_names()
    {
        Assert.Equal("habitflow", HabitFlow.Infrastructure.Data.DbNames.Schema);
        Assert.Equal("habitflow.users", HabitFlow.Infrastructure.Data.DbNames.Tables.Users);
        Assert.Equal("habitflow.habit_completions", HabitFlow.Infrastructure.Data.DbNames.Tables.HabitCompletions);
    }
}

public sealed class HabitLibraryDatabaseTests
{
    private static readonly string Root = Path.Combine("..", "..", "..", "..");

    [Fact]
    public void Complete_script_includes_habit_library_tables_and_seed()
    {
        var script = File.ReadAllText(Path.Combine(Root, "database", "script_completo.sql"));
        Assert.Contains("create table if not exists habitflow.habit_objectives", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists habitflow.habit_templates", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on conflict(slug) do update", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Beber água", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Habit_library_sql_uses_explicit_habitflow_schema()
    {
        var repo = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Infrastructure", "Repositories", "HabitTemplateRepository.cs"));
        Assert.Contains("habitflow.habit_templates", repo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" public.", repo, StringComparison.OrdinalIgnoreCase);
    }
}
