using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitCompletionRepository(SqlExecutor db) : IHabitCompletionRepository
{
    public async Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default) => (await db.QueryAsync<HabitCompletion>("select id, habit_id, user_id, completed_date, created_at from habitflow.habit_completions where user_id = @userId and (@from is null or completed_date >= @from)", new { userId, from }, ct)).ToList();
    public Task AddAsync(HabitCompletion c, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.habit_completions(id,habit_id,user_id,completed_date,created_at) values(@Id,@HabitId,@UserId,@CompletedDate,@CreatedAt) on conflict do nothing", c, ct);
    public Task DeleteAsync(Guid habitId, DateOnly date, CancellationToken ct = default) => db.ExecuteAsync("delete from habitflow.habit_completions where habit_id=@habitId and completed_date=@date", new { habitId, date }, ct);
}
