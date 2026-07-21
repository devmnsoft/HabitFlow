using System.Text.RegularExpressions;

namespace HabitFlow.Application.Operations;

public sealed record DeploymentEvent(Guid Id, string Version, string Environment, string? HostingMode, string Action, string Status, string? Notes, DateTime CreatedAt)
{
    public static DeploymentEvent Create(string version, string environment, string? hostingMode, string action, string status, string? notes = null) =>
        new(Guid.NewGuid(), version, environment, hostingMode, action, status, notes, DateTime.UtcNow);
}

public static class ConnectionStringMasker
{
    private static readonly Regex SecretPart = new("(?i)(Password|Pwd|User ID|Username|Token|Secret)\\s*=\\s*([^;]+)", RegexOptions.Compiled);
    public static string Mask(string? connectionString) => string.IsNullOrWhiteSpace(connectionString) ? "<não configurada>" : SecretPart.Replace(connectionString, "$1=***");
}

public static class HostingEnvironmentDetector
{
    public static string Detect(string? configuredMode, string? aspNetCoreModule, bool runningInContainer, string? processName)
    {
        if (!string.IsNullOrWhiteSpace(configuredMode)) return configuredMode;
        if (runningInContainer) return "Docker";
        if (!string.IsNullOrWhiteSpace(aspNetCoreModule)) return "Windows/IIS";
        if (string.Equals(processName, "dotnet", StringComparison.OrdinalIgnoreCase)) return "Standalone dotnet";
        return "Desconhecido";
    }
}

public sealed record BackupCommand(string Executable, string Arguments);

public static class BackupCommandBuilder
{
    public static BackupCommand BuildPgDump(string databaseName, string host, int port, string user, string outputPath, bool habitflowSchemaOnly = false)
    {
        var schemaArgument = habitflowSchemaOnly ? " --schema=habitflow" : string.Empty;
        return new("pg_dump", $"--format=custom --no-owner --no-privileges{schemaArgument} --host=\"{host}\" --port={port} --username=\"{user}\" --file=\"{outputPath}\" \"{databaseName}\"");
    }
}

public sealed record SmokeEndpointExpectation(string Path, int ExpectedStatus);

public static class SmokeTestPlan
{
    public static IReadOnlyList<SmokeEndpointExpectation> DefaultEndpoints =>
    [
        new("/", 200), new("/health", 200), new("/health/db", 200), new("/health/version", 200), new("/login", 200)
    ];
}
