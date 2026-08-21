using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserChallengeRepository(SqlExecutor db) : IUserChallengeRepository
{
    private const string Projection = "c.id,c.client_id,c.user_id,c.habit_id,c.name,c.description,c.duration_days,c.start_date,c.end_date,c.status,c.created_at,c.updated_at,c.completed_at,count(distinct hc.completed_date)::int as progress_days";

    public async Task<IReadOnlyList<UserChallenge>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserChallenge>($"select {Projection} from habitflow.user_challenges c left join habitflow.habit_completions hc on hc.client_id=c.client_id and hc.user_id=c.user_id and hc.habit_id=c.habit_id and hc.completed_date between c.start_date and c.end_date where c.client_id=@clientId and c.user_id=@userId group by c.id order by (c.status='Active') desc,c.created_at desc", new { clientId,userId },ct)).ToList();

    public Task<UserChallenge?> GetActiveAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => QueryOne("c.habit_id=@habitId and c.status='Active'",new { clientId,userId,habitId },ct);
    public Task<UserChallenge?> GetAsync(Guid clientId, Guid userId, Guid challengeId, CancellationToken ct = default) => QueryOne("c.id=@challengeId",new { clientId,userId,challengeId },ct);

    public Task CreateAsync(UserChallenge c, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.user_challenges(id,client_id,user_id,habit_id,name,description,duration_days,start_date,end_date,status,created_at,updated_at) values(@Id,@ClientId,@UserId,@HabitId,@Name,@Description,@DurationDays,@StartDate,@EndDate,@Status,@CreatedAt,@UpdatedAt)",new { c.Id,c.ClientId,c.UserId,c.HabitId,c.Name,c.Description,c.DurationDays,c.StartDate,c.EndDate,Status=c.Status.ToString(),c.CreatedAt,c.UpdatedAt },ct);

    public async Task<bool> SetStatusAsync(Guid clientId, Guid userId, Guid challengeId, UserChallengeStatus status, DateTime now, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.user_challenges set status=@status,updated_at=@now,completed_at=case when @status='Completed' then @now else completed_at end where id=@challengeId and client_id=@clientId and user_id=@userId and status='Active'",new { clientId,userId,challengeId,status=status.ToString(),now },ct)>0;

    private Task<UserChallenge?> QueryOne(string extra,object args,CancellationToken ct) => db.QuerySingleOrDefaultAsync<UserChallenge>($"select {Projection} from habitflow.user_challenges c left join habitflow.habit_completions hc on hc.client_id=c.client_id and hc.user_id=c.user_id and hc.habit_id=c.habit_id and hc.completed_date between c.start_date and c.end_date where c.client_id=@clientId and c.user_id=@userId and {extra} group by c.id limit 1",args,ct);
}
