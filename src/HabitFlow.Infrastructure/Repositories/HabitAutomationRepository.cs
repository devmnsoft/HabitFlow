using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitAutomationRepository(SqlExecutor db) : IHabitAutomationRepository
{
    public async Task<IReadOnlyList<HabitAutomation>> ListForUserAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitAutomationRow>("""
            select id as "Id", client_id as "ClientId", user_id as "UserId", habit_id as "HabitId",
                   type as "Type", frequency as "Frequency", status as "Status",
                   quiet_hours_start as "QuietHoursStart", quiet_hours_end as "QuietHoursEnd",
                   settings_json as "SettingsJson", last_triggered_at as "LastTriggeredAt",
                   created_at as "CreatedAt", updated_at as "UpdatedAt"
            from habitflow.habit_automations
            where client_id = @clientId and user_id = @userId
            order by created_at desc
            """, new { clientId, userId }, ct);
        return rows.Select(Map).ToList();
    }

    public Task CreateAsync(HabitAutomation automation, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.habit_automations(id, client_id, user_id, habit_id, type, frequency, status,
                quiet_hours_start, quiet_hours_end, settings_json, last_triggered_at, created_at, updated_at)
            values(@Id, @ClientId, @UserId, @HabitId, @Type, @Frequency, @Status,
                @QuietHoursStart, @QuietHoursEnd, @SettingsJson, @LastTriggeredAt, @CreatedAt, @UpdatedAt)
            """, new
            {
                automation.Id,
                automation.ClientId,
                automation.UserId,
                automation.HabitId,
                Type = automation.Type.ToString(),
                automation.Frequency,
                Status = automation.Status.ToString(),
                QuietHoursStart = automation.QuietHoursStart.ToString("HH:mm"),
                QuietHoursEnd = automation.QuietHoursEnd.ToString("HH:mm"),
                automation.SettingsJson,
                automation.LastTriggeredAt,
                automation.CreatedAt,
                automation.UpdatedAt
            }, ct);

    public Task UpdateStatusAsync(Guid id, Guid clientId, Guid userId, HabitAutomationStatus status, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            update habitflow.habit_automations
               set status = @status, updated_at = now()
             where id = @id and client_id = @clientId and user_id = @userId
            """, new { id, clientId, userId, status = status.ToString() }, ct);

    private static HabitAutomation Map(HabitAutomationRow row) => new(row.Id, row.ClientId, row.UserId, row.HabitId,
        Enum.TryParse<HabitAutomationType>(row.Type, true, out var type) ? type : HabitAutomationType.SmartReminder,
        row.Frequency,
        Enum.TryParse<HabitAutomationStatus>(row.Status, true, out var status) ? status : HabitAutomationStatus.Error,
        TimeOnly.TryParse(row.QuietHoursStart, out var start) ? start : new TimeOnly(22, 0),
        TimeOnly.TryParse(row.QuietHoursEnd, out var end) ? end : new TimeOnly(7, 0),
        row.SettingsJson, row.LastTriggeredAt, row.CreatedAt, row.UpdatedAt);

    private sealed class HabitAutomationRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid? HabitId { get; init; }
        public string Type { get; init; } = "";
        public string Frequency { get; init; } = "";
        public string Status { get; init; } = "";
        public string QuietHoursStart { get; init; } = "";
        public string QuietHoursEnd { get; init; } = "";
        public string? SettingsJson { get; init; }
        public DateTime? LastTriggeredAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
