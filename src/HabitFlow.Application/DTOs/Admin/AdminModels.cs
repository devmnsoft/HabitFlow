using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record AdminUserFilter(string? Search = null, UserPlan? Plan = null, AccountStatus? AccountStatus = null, RiskStatus? RiskStatus = null, bool? WantsPremiumNotice = null, DateTime? CreatedFrom = null, DateTime? CreatedTo = null, DateTime? LastLoginFrom = null, DateTime? LastLoginTo = null, int Page = 1, int PageSize = 20);
public sealed record SupportTicketFilter(string? Search = null, string? Status = null, string? Priority = null, string? Type = null, DateTime? CreatedFrom = null, DateTime? CreatedTo = null, int Page = 1, int PageSize = 20);
public sealed record LgpdRequestFilter(string? Type = null, string? Status = null, DateTime? CreatedFrom = null, DateTime? CreatedTo = null, int Page = 1, int PageSize = 20);
public sealed record AuditLogFilter(string? Severity = null, string? Action = null, string? UserEmail = null, DateTime? CreatedFrom = null, DateTime? CreatedTo = null, int Page = 1, int PageSize = 20);

public sealed record AdminUserListItem(Guid Id, string Name, string Email, UserPlan Plan, PlanStatus PlanStatus, AccountStatus AccountStatus, RiskStatus RiskStatus, bool WantsPremiumNotice, int ActiveHabits, int Completions, DateTime? LastLoginAt, DateTime CreatedAt);
public sealed record AdminUserDetail(AdminUserListItem User, IReadOnlyList<AdminUserNote> Notes, IReadOnlyList<object> Tickets, IReadOnlyList<object> LgpdRequests, IReadOnlyList<object> RecentLogs);
public sealed record AdminDashboardDto(long TotalUsers, long ActiveUsers7Days, long NewUsers7Days, long ActiveHabits, long Completions7Days, long OpenTickets, long OpenLgpdRequests, long CriticalErrors24h, long PremiumLeads, decimal PotentialMonthlyRevenue, string SystemStatus, IReadOnlyList<AdminUserListItem> LatestUsers);
public sealed record SystemHealthSummary(string Status, bool TelegramEnabled, bool HasPendingLgpd, long CriticalErrors24h);
public sealed record GlobalMetrics(long Users, long Habits, long Completions, long PremiumUsers);
public sealed record CommercialFunnel(long RegisteredUsers, long WithFirstHabit, long Active7Days, long FreeLimitReached, long InterestedPremium, long ActivePremium);
public sealed record PremiumLead(Guid UserId, string Name, string Email, string Reason, int ActiveHabits, int Completions30Days, DateTime? LastLoginAt, UserPlan Plan);
public sealed record FinancialSummary(decimal RealRevenue, decimal PotentialMonthlyRevenue, decimal PotentialAnnualRevenue, long ActivePremium, long PremiumLeads, IReadOnlyList<object> RecentEvents);
