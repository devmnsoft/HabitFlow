using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitReminderRepository(SqlExecutor db) : IHabitReminderRepository
{
    private const string Select = "select r.id,r.client_id,r.user_id,r.habit_id,h.name habit_name,r.reminder_time,r.timezone,coalesce(r.days_of_week,'{}') days_of_week,r.is_active,r.last_triggered_at,r.next_trigger_at,r.created_at,r.updated_at from habitflow.habit_reminders r join habitflow.habits h on h.id=r.habit_id and h.client_id=r.client_id and h.user_id=r.user_id";
    public async Task<IReadOnlyList<HabitReminder>> ListAsync(Guid clientId, Guid userId, Guid? habitId = null, CancellationToken ct = default) =>
        (await db.QueryAsync<HabitReminder>(Select + " where r.client_id=@clientId and r.user_id=@userId and (@habitId is null or r.habit_id=@habitId) order by r.is_active desc,r.next_trigger_at nulls last", new { clientId, userId, habitId }, ct)).ToList();
    public Task<HabitReminder?> GetOwnedAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitReminder>(Select + " where r.client_id=@clientId and r.user_id=@userId and r.id=@id", new { clientId, userId, id }, ct);
    public async Task<bool> HabitBelongsToUserAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) =>
        await db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.habits where id=@habitId and client_id=@clientId and user_id=@userId", new { clientId,userId,habitId }, ct) == 1;
    public async Task CreateAsync(HabitReminder r, CancellationToken ct = default) => _ = await db.ExecuteAsync("insert into habitflow.habit_reminders(id,client_id,user_id,habit_id,reminder_time,timezone,days_of_week,is_active,next_trigger_at,created_at,updated_at) values(@Id,@ClientId,@UserId,@HabitId,@ReminderTime,@Timezone,@DaysOfWeek,@IsActive,@NextTriggerAt,@CreatedAt,@UpdatedAt)", r, ct);
    public async Task<bool> SetActiveAsync(Guid clientId, Guid userId, Guid id, bool active, DateTimeOffset? next, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.habit_reminders set is_active=@active,next_trigger_at=@next,updated_at=now() where id=@id and client_id=@clientId and user_id=@userId", new { clientId,userId,id,active,next }, ct) == 1;
    public async Task<bool> SnoozeAsync(Guid clientId, Guid userId, Guid id, DateTimeOffset next, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.habit_reminders set next_trigger_at=@next,updated_at=now() where id=@id and client_id=@clientId and user_id=@userId and is_active", new { clientId,userId,id,next }, ct) == 1;
    public async Task<bool> DeleteAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        await db.ExecuteAsync("delete from habitflow.habit_reminders where id=@id and client_id=@clientId and user_id=@userId", new { clientId,userId,id }, ct) == 1;
}
