namespace HabitFlow.Domain;

public sealed record OperationsSnapshot(int TotalClients, int ActiveClients, int BlockedClients, int OverdueClients,
    int ActiveUsers, int OpenTickets, int CriticalTickets, int RecentErrors, int FailedWebhooks,
    int FailedIntegrations, long ApiRequests24h, int PendingSubscriptions, int SecurityAlerts,
    string SystemStatus, IReadOnlyList<ModuleUsage> Modules, IReadOnlyList<TenantRiskRow> Tenants,
    IReadOnlyList<OperationalAlertRow> CriticalAlerts, DateTime CheckedAt);
public sealed record ModuleUsage(string Module, long Uses);
public sealed record TenantRiskRow(Guid TenantId, string ClientName, string Status, string Plan, int EnabledModules,
    int ActiveUsers, int UsedLimit, int? Limit, int OpenTickets, int PendingPayments, DateTime? LastAccess,
    int RecentErrors, string Risk, string RiskReason);
public sealed record OperationalAlertRow(Guid Id, Guid? TenantId, string? ClientName, string Type, string Severity,
    string Title, int Occurrences, DateTime LastOccurredAt, string Status);
public sealed record StructuredLogFilter(DateTime? From = null, DateTime? To = null, Guid? TenantId = null,
    Guid? UserId = null, string? Module = null, string? Severity = null, string? CorrelationId = null, int Page = 1);
public sealed record StructuredLogRow(Guid Id, DateTime CreatedAt, string Severity, string EventName, string Module,
    Guid? TenantId, string? ClientName, Guid? UserId, string? UserName, string CorrelationId, string Message, string Details);
public sealed record HealthCheckRow(string Name, string Status, string Message, string Severity, DateTime CheckedAt, string Recommendation);
public sealed record OperationsHealthReport(string OverallStatus, string Version, string Environment, IReadOnlyList<HealthCheckRow> Checks);

public static class TenantRiskCalculator
{
    public static (string Risk, string Reason) Calculate(string status, int errors, int pendingPayments, int openTickets, int used, int? limit)
    {
        var utilization = limit is > 0 ? used * 100 / limit.Value : 0;
        var score = (status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? 100 : 0) +
                    Math.Min(errors * 12, 48) + Math.Min(pendingPayments * 25, 50) + Math.Min(openTickets * 5, 20) +
                    (utilization >= 90 ? 20 : 0);
        var risk = score >= 100 ? "Crítico" : score >= 60 ? "Alto" : score >= 25 ? "Médio" : "Baixo";
        var reason = status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? "Cliente bloqueado" : errors >= 3 ? "Erros recorrentes" :
            pendingPayments > 0 ? "Pagamento pendente" : utilization >= 90 ? "Limite acima de 90%" : openTickets > 0 ? "Chamados em aberto" : "Sem sinais de risco";
        return (risk, reason);
    }
}
