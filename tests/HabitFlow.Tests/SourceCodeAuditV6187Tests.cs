using Xunit;

namespace HabitFlow.Tests;

public sealed class SourceCodeAuditV6187Tests
{
    [Fact]
    public void Client_onboarding_uses_fixed_sql_and_resets_stale_completion_time()
    {
        var root = RepositoryRootLocator.Find();
        var source = File.ReadAllText(Path.Combine(root, "src", "HabitFlow.Infrastructure", "Repositories", "SaaSOperationsRepositories.cs"));

        Assert.DoesNotContain("set {step}", source, StringComparison.Ordinal);
        Assert.Contains("completed_at=case when @completed then now() else null end", source, StringComparison.Ordinal);
        Assert.Contains("_ => throw new ArgumentOutOfRangeException", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Required_operational_events_have_stable_structured_names()
    {
        var root = RepositoryRootLocator.Find();
        var source = File.ReadAllText(Path.Combine(root, "src", "HabitFlow.Application", "Observability", "ApplicationEvents.cs"));
        var names = new[]
        {
            "audit.issue_found", "audit.issue_fixed", "validation.failed", "habit.operation_failed",
            "goal.operation_failed", "routine.operation_failed", "notification.operation_failed",
            "tenant.access_denied", "system.health_checked"
        };

        foreach (var name in names)
            Assert.Contains($"\"{name}\"", source, StringComparison.Ordinal);
    }
}
