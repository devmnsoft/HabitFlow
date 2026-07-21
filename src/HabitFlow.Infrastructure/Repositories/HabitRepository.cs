using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitRepository(SqlExecutor db) : IHabitRepository
{
    private const string Columns = "id, user_id, name, color, category, is_archived, archived_at, created_at, updated_at, frequency_type, target_per_week, reminder_time, notes, sort_order";

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.habits where user_id = @userId and is_archived = false", new { userId }, ct)!;
    public async Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<Habit>("select " + Columns + " from habitflow.habits where user_id = @userId order by is_archived, sort_order, created_at desc", new { userId }, ct)).ToList();
    public Task<Habit?> GetAsync(Guid id, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<Habit>("select " + Columns + " from habitflow.habits where id = @id", new { id }, ct);
    public Task CreateAsync(Habit h, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.habits(id,user_id,name,color,category,is_archived,archived_at,created_at,updated_at,frequency_type,target_per_week,reminder_time,notes,sort_order) values(@Id,@UserId,@Name,@Color,@Category,@IsArchived,@ArchivedAt,@CreatedAt,@UpdatedAt,@FrequencyType,@TargetPerWeek,@ReminderTime,@Notes,@SortOrder)", new { h.Id, h.UserId, h.Name, h.Color, h.Category, h.IsArchived, h.ArchivedAt, h.CreatedAt, h.UpdatedAt, FrequencyType = h.FrequencyType.ToString(), h.TargetPerWeek, h.ReminderTime, h.Notes, h.SortOrder }, ct);
    public Task UpdateAsync(Habit h, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.habits set name=@Name,color=@Color,category=@Category,is_archived=@IsArchived,archived_at=@ArchivedAt,updated_at=@UpdatedAt,frequency_type=@FrequencyType,target_per_week=@TargetPerWeek,reminder_time=@ReminderTime,notes=@Notes,sort_order=@SortOrder where id=@Id", new { h.Id, h.Name, h.Color, h.Category, h.IsArchived, h.ArchivedAt, h.UpdatedAt, FrequencyType = h.FrequencyType.ToString(), h.TargetPerWeek, h.ReminderTime, h.Notes, h.SortOrder }, ct);
}
