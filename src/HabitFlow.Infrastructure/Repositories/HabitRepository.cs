using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitRepository(SqlExecutor db) : IHabitRepository
{
    private const string Columns = "id, user_id, name, color, category, is_archived, archived_at, created_at, updated_at";

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.habits where user_id = @userId and is_archived = false", new { userId }, ct)!;
    public async Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<Habit>("select " + Columns + " from habitflow.habits where user_id = @userId order by is_archived, created_at desc", new { userId }, ct)).ToList();
    public Task<Habit?> GetAsync(Guid id, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<Habit>("select " + Columns + " from habitflow.habits where id = @id", new { id }, ct);
    public Task CreateAsync(Habit h, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.habits(id,user_id,name,color,category,is_archived,archived_at,created_at,updated_at) values(@Id,@UserId,@Name,@Color,@Category,@IsArchived,@ArchivedAt,@CreatedAt,@UpdatedAt)", h, ct);
    public Task UpdateAsync(Habit h, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.habits set name=@Name,color=@Color,category=@Category,is_archived=@IsArchived,archived_at=@ArchivedAt,updated_at=@UpdatedAt where id=@Id", h, ct);
}
