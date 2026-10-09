namespace HabitFlow.Domain;

public static class HealthStatusConstants
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
    public const string Disabled = "Disabled";
    public const string NotConfigured = "NotConfigured";

    public static readonly IReadOnlyList<string> All = [Healthy, Degraded, Unhealthy, Disabled, NotConfigured];
}

public sealed record SystemHealthStatusItem(
    string ComponentName,
    string Status,
    int DurationMs,
    string? Details,
    string? ErrorMessage,
    string? OperationalRecommendation,
    DateTime LastCheckedAt
);

public sealed record ComprehensiveHealthReport(
    string OverallStatus,
    DateTime GeneratedAt,
    int DurationMs,
    IReadOnlyList<SystemHealthStatusItem> Components,
    IReadOnlyList<string> Recommendations
);

public interface ISystemHealthRepository
{
    Task<bool> PingDatabaseAsync(CancellationToken ct = default);
    Task SaveHealthCheckAsync(SystemHealthStatusItem item, CancellationToken ct = default);
    Task<IReadOnlyList<SystemHealthStatusItem>> GetLatestHealthChecksAsync(CancellationToken ct = default);
    Task RecordHistoryAsync(string componentName, string status, int durationMs, string? error, CancellationToken ct = default);
    Task<IReadOnlyList<SystemHealthStatusItem>> GetHealthHistoryAsync(string? componentName = null, int limit = 50, CancellationToken ct = default);
}
