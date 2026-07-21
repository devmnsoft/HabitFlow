using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserReportRepository(SqlExecutor db) : IUserReportRepository
{
    public Task CreateAsync(UserReport r, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.user_reports(id,user_id,report_type,period_start,period_end,summary,created_at) values(@Id,@UserId,@ReportType,@PeriodStart,@PeriodEnd,cast(@Summary as jsonb),@CreatedAt)", r, ct);
    public async Task<IReadOnlyList<UserReport>> ListByUserAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<UserReport>("select id,user_id,report_type,period_start,period_end,summary::text as summary,created_at from habitflow.user_reports where user_id=@userId order by created_at desc", new { userId }, ct)).ToList();
}
