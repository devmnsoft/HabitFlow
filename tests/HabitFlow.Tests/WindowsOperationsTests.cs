using HabitFlow.Application.Operations;
using Xunit;

namespace HabitFlow.Tests;

public sealed class WindowsOperationsTests
{
    [Fact]
    public void Connection_string_masker_hides_password_and_user()
    {
        var masked = ConnectionStringMasker.Mask("Host=localhost;Username=postgres;Password=secret");
        Assert.DoesNotContain("secret", masked);
        Assert.DoesNotContain("postgres", masked);
        Assert.Contains("Password=***", masked);
    }

    [Fact]
    public void Environment_detector_prefers_configured_mode()
    {
        Assert.Equal("Windows/IIS", HostingEnvironmentDetector.Detect("Windows/IIS", null, false, "dotnet"));
        Assert.Equal("Docker", HostingEnvironmentDetector.Detect(null, null, true, "dotnet"));
    }

    [Fact]
    public void Backup_command_builder_uses_pg_dump_custom_format()
    {
        var cmd = BackupCommandBuilder.BuildPgDump("habitflow", "localhost", 5432, "postgres", "backup.dump");
        Assert.Equal("pg_dump", cmd.Executable);
        Assert.Contains("--format=custom", cmd.Arguments);
        Assert.Contains("backup.dump", cmd.Arguments);
    }

    [Fact]
    public void Backup_command_builder_can_restrict_dump_to_habitflow_schema()
    {
        var cmd = BackupCommandBuilder.BuildPgDump("habitflow", "localhost", 5432, "postgres", "backup.dump", habitflowSchemaOnly: true);
        Assert.Contains("--schema=habitflow", cmd.Arguments);
    }

    [Fact]
    public void Deployment_event_sets_required_metadata()
    {
        var ev = DeploymentEvent.Create("v4.4", "Production", "Windows/IIS", "backup", "success");
        Assert.NotEqual(Guid.Empty, ev.Id);
        Assert.Equal("backup", ev.Action);
    }

    [Fact]
    public void Smoke_test_plan_contains_health_endpoints()
    {
        Assert.Contains(SmokeTestPlan.DefaultEndpoints, e => e.Path == "/health/db" && e.ExpectedStatus == 200);
    }
}
