using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitReminderRepository(SqlExecutor db) : IHabitReminderRepository
{
    private const string Select = """
        select
            r.id as "Id",
            r.client_id as "ClientId",
            r.user_id as "UserId",
            r.habit_id as "HabitId",
            h.name as "HabitName",
            r.reminder_time as "ReminderTime",
            r.timezone as "Timezone",
            coalesce(r.days_of_week, array[]::integer[]) as "DaysOfWeek",
            r.is_active as "IsActive",
            r.last_triggered_at as "LastTriggeredAt",
            r.next_trigger_at as "NextTriggerAt",
            r.created_at as "CreatedAt",
            r.updated_at as "UpdatedAt"
        from habitflow.habit_reminders r
        join habitflow.habits h
          on h.id = r.habit_id
         and h.client_id = r.client_id
         and h.user_id = r.user_id
        """;

    public async Task<IReadOnlyList<HabitReminder>> ListAsync(Guid clientId, Guid userId, Guid? habitId = null, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitReminderRow>(Select + " where r.client_id=@clientId and r.user_id=@userId and (@habitId is null or r.habit_id=@habitId) order by r.is_active desc,r.next_trigger_at nulls last", new { clientId, userId, habitId }, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<HabitReminder?> GetOwnedAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<HabitReminderRow>(Select + " where r.client_id=@clientId and r.user_id=@userId and r.id=@id", new { clientId, userId, id }, ct);
        return row is null ? null : Map(row);
    }

    public async Task<bool> HabitBelongsToUserAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) =>
        await db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.habits where id=@habitId and client_id=@clientId and user_id=@userId", new { clientId,userId,habitId }, ct) == 1;

    public async Task CreateAsync(HabitReminder r, CancellationToken ct = default)
    {
        var parameters = new
        {
            r.Id, r.ClientId, r.UserId, r.HabitId, r.ReminderTime, r.Timezone, r.DaysOfWeek, r.IsActive,
            NextTriggerAt = ToUtcDateTime(r.NextTriggerAt), r.CreatedAt, r.UpdatedAt
        };
        _ = await db.ExecuteAsync("insert into habitflow.habit_reminders(id,client_id,user_id,habit_id,reminder_time,timezone,days_of_week,is_active,next_trigger_at,created_at,updated_at) values(@Id,@ClientId,@UserId,@HabitId,@ReminderTime,@Timezone,@DaysOfWeek,@IsActive,@NextTriggerAt,@CreatedAt,@UpdatedAt)", parameters, ct);
    }

    public async Task<bool> SetActiveAsync(Guid clientId, Guid userId, Guid id, bool active, DateTimeOffset? next, CancellationToken ct = default)
    {
        var nextAt = ToUtcDateTime(next);
        return await db.ExecuteAsync("update habitflow.habit_reminders set is_active=@active,next_trigger_at=@nextAt,updated_at=now() where id=@id and client_id=@clientId and user_id=@userId", new { clientId,userId,id,active,nextAt }, ct) == 1;
    }

    public async Task<bool> SnoozeAsync(Guid clientId, Guid userId, Guid id, DateTimeOffset next, CancellationToken ct = default)
    {
        var nextAt = next.UtcDateTime;
        return await db.ExecuteAsync("update habitflow.habit_reminders set next_trigger_at=@nextAt,updated_at=now() where id=@id and client_id=@clientId and user_id=@userId and is_active", new { clientId,userId,id,nextAt }, ct) == 1;
    }

    public async Task<bool> DeleteAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        await db.ExecuteAsync("delete from habitflow.habit_reminders where id=@id and client_id=@clientId and user_id=@userId", new { clientId,userId,id }, ct) == 1;

    private static HabitReminder Map(HabitReminderRow row) => new(row.Id, row.ClientId, row.UserId, row.HabitId,
        row.HabitName, row.ReminderTime, row.Timezone, row.DaysOfWeek ?? [], row.IsActive,
        ToUtcOffset(row.LastTriggeredAt), ToUtcOffset(row.NextTriggerAt), row.CreatedAt, row.UpdatedAt);

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
    {
        if (value is null) return null;
        var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static DateTime? ToUtcDateTime(DateTimeOffset? value) => value?.UtcDateTime;

    private sealed class HabitReminderRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public string HabitName { get; init; } = "";
        public TimeOnly ReminderTime { get; init; }
        public string Timezone { get; init; } = "";
        public int[] DaysOfWeek { get; init; } = [];
        public bool IsActive { get; init; }
        public DateTime? LastTriggeredAt { get; init; }
        public DateTime? NextTriggerAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
