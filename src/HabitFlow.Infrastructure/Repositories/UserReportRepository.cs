using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserReportRepository(SqlExecutor db) : IUserReportRepository
{
    public Task CreateAsync(UserReport r, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.user_reports(id,client_id,user_id,report_type,period_start,period_end,summary,algorithm_version,created_at)
        values(@Id,@ClientId,@UserId,@ReportType,@PeriodStart,@PeriodEnd,cast(@Summary as jsonb),@AlgorithmVersion,@CreatedAt)
        on conflict(client_id,user_id,report_type,period_start,algorithm_version) do nothing
        """, r, ct);
    public async Task<IReadOnlyList<UserReport>> ListByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserReport>("select id,client_id,user_id,report_type,period_start,period_end,summary::text as summary,algorithm_version,created_at from habitflow.user_reports where client_id=@clientId and user_id=@userId order by period_start desc,created_at desc", new { clientId, userId }, ct)).ToList();
}
