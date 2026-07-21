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
    public void Complete_script_contains_required_tables(string table)
    {
        Assert.Contains($"create table if not exists {table}", Script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_script_is_self_contained_and_contains_key_constraints()
    {
        Assert.DoesNotContain("\\i", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("constraint uq_habit_completions_habit_date unique", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ix_system_audit_logs_created_at", Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into habitflow.system_settings", Script, StringComparison.OrdinalIgnoreCase);
    }
}
