using HabitFlow.Domain;

namespace HabitFlow.Application;

public interface IAdminUserRepository
{
    Task<(IReadOnlyList<AdminUserListItem> Items, int TotalCount)> SearchUsersAsync(AdminUserFilter filter, CancellationToken ct = default);
    Task<AdminUserListItem?> GetUserListItemAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAccountStatusAsync(Guid userId, AccountStatus status, string reason, CancellationToken ct = default);
    Task UpdateRiskStatusAsync(Guid userId, RiskStatus status, string reason, CancellationToken ct = default);
    Task UpdatePlanAsync(Guid userId, UserPlan plan, PlanStatus planStatus, string reason, CancellationToken ct = default);
    Task AddNoteAsync(AdminUserNote note, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUserNote>> GetNotesAsync(Guid userId, CancellationToken ct = default);
}
public interface IAdminMetricsRepository
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<GlobalMetrics> GetGlobalMetricsAsync(CancellationToken ct = default);
    Task<CommercialFunnel> GetCommercialFunnelAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<PremiumLead> Items, int TotalCount)> GetPremiumLeadsAsync(AdminUserFilter filter, CancellationToken ct = default);
    Task<FinancialSummary> GetFinancialSummaryAsync(CancellationToken ct = default);
}
public interface IAdminSupportRepository { Task<(IReadOnlyList<object> Items, int TotalCount)> SearchTicketsAsync(SupportTicketFilter filter, CancellationToken ct = default); Task<object?> GetTicketDetailAsync(Guid ticketId, CancellationToken ct = default); Task UpdateTicketStatusAsync(Guid ticketId, string status, string? message, CancellationToken ct = default); }
public interface IAdminLgpdRepository { Task<(IReadOnlyList<object> Items, int TotalCount)> SearchRequestsAsync(LgpdRequestFilter filter, CancellationToken ct = default); Task UpdateRequestStatusAsync(Guid requestId, string status, string? notes, CancellationToken ct = default); }
public interface IAdminAuditQueryRepository { Task<(IReadOnlyList<object> Items, int TotalCount)> SearchSystemLogsAsync(AuditLogFilter filter, CancellationToken ct = default); Task<(IReadOnlyList<object> Items, int TotalCount)> SearchAdminLogsAsync(AuditLogFilter filter, CancellationToken ct = default); Task MarkSystemLogAsReadAsync(Guid logId, CancellationToken ct = default); }
public interface IAdminExportRepository { Task AddAsync(AdminExport export, CancellationToken ct = default); }
public interface IAdminBillingRepository { Task<FinancialSummary> GetFinancialSummaryAsync(CancellationToken ct = default); }
