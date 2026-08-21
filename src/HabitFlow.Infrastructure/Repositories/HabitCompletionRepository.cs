using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitCompletionRepository(SqlExecutor db) : IHabitCompletionRepository
{
    public async Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default) => (await db.QueryAsync<HabitCompletion>("select id, habit_id, user_id, completed_date, created_at from habitflow.habit_completions where user_id = @userId and (@from is null or completed_date >= @from)", new { userId, from }, ct)).ToList();
    public async Task<IReadOnlyList<HabitCompletion>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) => (await db.QueryAsync<HabitCompletion>("select id, habit_id, user_id, completed_date, created_at from habitflow.habit_completions where client_id = @clientId and user_id = @userId and completed_date between @from and @to", new { clientId, userId, from, to }, ct)).ToList();
    public Task AddAsync(HabitCompletion c, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.habit_completions(id,habit_id,user_id,completed_date,created_at) values(@Id,@HabitId,@UserId,@CompletedDate,@CreatedAt) on conflict do nothing", c, ct);
    public Task DeleteAsync(Guid habitId, Guid userId, DateOnly date, CancellationToken ct = default) => db.ExecuteAsync("delete from habitflow.habit_completions where habit_id=@habitId and user_id=@userId and completed_date=@date", new { habitId, userId, date }, ct);

    public async Task<CompletionMutationResult> AddIfMissingAsync(Guid clientId, Guid userId, Guid habitId,
        DateOnly localDate, Guid completionId, CancellationToken ct = default)
    {
        var createdId = await db.QuerySingleOrDefaultAsync<Guid?>("""
            insert into habitflow.habit_completions(id, client_id, habit_id, user_id, completed_date, created_at)
            select @completionId, @clientId, h.id, @userId, @localDate, now()
              from habitflow.habits h
              join habitflow.users u on u.id = @userId and u.client_id = @clientId
             where h.id = @habitId and h.user_id = @userId and h.client_id = @clientId
               and h.is_archived = false and h.is_paused = false
            on conflict (habit_id, completed_date) do nothing
            returning id
            """, new { clientId, userId, habitId, localDate, completionId }, ct);

        return new(createdId, createdId.HasValue, false, true, localDate);
    }

    public async Task<CompletionMutationResult> DeleteIfExistsAsync(Guid clientId, Guid userId, Guid habitId,
        DateOnly localDate, CancellationToken ct = default)
    {
        var deletedId = await db.QuerySingleOrDefaultAsync<Guid?>("""
            delete from habitflow.habit_completions
             where client_id = @clientId and user_id = @userId
               and habit_id = @habitId and completed_date = @localDate
            returning id
            """, new { clientId, userId, habitId, localDate }, ct);

        return new(deletedId, false, deletedId.HasValue, false, localDate);
    }
}
