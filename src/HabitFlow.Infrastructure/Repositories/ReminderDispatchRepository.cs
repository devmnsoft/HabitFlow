using Dapper;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ReminderDispatchRepository(DbConnectionFactory connections) : IReminderDispatchRepository
{
    public async Task<IReadOnlyList<ReminderDispatchCandidate>> ClaimAsync(DateTimeOffset now, int batchSize,
        string workerId, TimeSpan lease, CancellationToken ct = default)
    {
        using var connection = await connections.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        const string sql = """
            with due as (
              select r.id, r.client_id, r.user_id, r.habit_id, h.name, r.reminder_time,
                     r.timezone, coalesce(r.days_of_week,array[]::integer[]) days_of_week,
                     r.next_trigger_at
                from habitflow.habit_reminders r
                join habitflow.habits h on h.id=r.habit_id and h.client_id=r.client_id and h.user_id=r.user_id
                join habitflow.users u on u.id=r.user_id and u.client_id=r.client_id
                join habitflow.clients c on c.id=r.client_id
               where r.is_active and r.next_trigger_at <= @now and not h.is_archived
                 and not coalesce(h.is_paused,false) and u.account_status='Active'
                 and (r.locked_until is null or r.locked_until < @now)
               order by r.next_trigger_at, r.id
               for update of r skip locked limit @batchSize
            ), leased as (
              update habitflow.habit_reminders r
                 set locked_by=@workerId, locked_until=@leaseUntil, updated_at=@now
                from due where r.id=due.id
              returning due.*
            ), dispatch as (
              insert into habitflow.reminder_dispatches
                (id,client_id,user_id,habit_reminder_id,habit_id,scheduled_for_utc,channel,status,
                 attempt_count,next_attempt_at,locked_by,locked_until,correlation_id,created_at)
              select gen_random_uuid(),client_id,user_id,id,habit_id,next_trigger_at,'in_app','Processing',
                     0,@now,@workerId,@leaseUntil,gen_random_uuid(),@now from leased
              on conflict(habit_reminder_id,scheduled_for_utc,channel) do update
                set locked_by=excluded.locked_by, locked_until=excluded.locked_until, status='Processing'
                where habitflow.reminder_dispatches.status in ('Pending','Retry')
                  and (habitflow.reminder_dispatches.locked_until is null or habitflow.reminder_dispatches.locked_until < @now)
              returning *
            )
            select d.id "DispatchId", l.id "ReminderId", l.client_id "ClientId", l.user_id "UserId",
                   l.habit_id "HabitId", l.name "HabitName", l.reminder_time "ReminderTime",
                   l.timezone "Timezone", l.days_of_week "DaysOfWeek", d.scheduled_for_utc "ScheduledFor",
                   d.attempt_count "AttemptCount", d.correlation_id "CorrelationId"
              from dispatch d join leased l on l.id=d.habit_reminder_id
            """;
        // Do not ask Dapper to materialize the immutable domain record directly. PostgreSQL
        // returns timestamp columns as DateTime and the record constructor expects
        // DateTimeOffset; constructor binding therefore changes with provider/Dapper versions.
        // A mutable persistence projection keeps that conversion explicit and deterministic.
        var rows = (await connection.QueryAsync<ReminderDispatchCandidateRow>(new CommandDefinition(sql,
            new { now = now.UtcDateTime, batchSize, workerId, leaseUntil = now.Add(lease).UtcDateTime },
            transaction, cancellationToken: ct))).AsList();
        transaction.Commit();
        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task CompleteAsync(ReminderDispatchCandidate candidate, DateTimeOffset nextOccurrence,
        DateTimeOffset now, CancellationToken ct = default)
    {
        using var connection = await connections.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        const string sql = """
            insert into habitflow.notifications
              (id,client_id,user_id,type,category,title,message,severity,action_url,related_entity_type,
               related_entity_id,deduplication_key,created_at)
            values(gen_random_uuid(),@ClientId,@UserId,'Reminder','Reminder',@Title,@Message,'Info',
                   @ActionUrl,'Habit',@HabitId,@DeduplicationKey,@Now)
            on conflict(client_id,user_id,deduplication_key) where deduplication_key is not null do nothing;

            update habitflow.habit_reminders set last_triggered_at=@ScheduledFor,next_trigger_at=@Next,
                   locked_by=null,locked_until=null,updated_at=@Now
             where id=@ReminderId and client_id=@ClientId and user_id=@UserId and habit_id=@HabitId;

            update habitflow.reminder_dispatches set status='Delivered',attempt_count=attempt_count+1,
                   processed_at=@Now,locked_by=null,locked_until=null,error_code=null,last_error_at=null
             where id=@DispatchId and client_id=@ClientId and user_id=@UserId and habit_id=@HabitId
               and habit_reminder_id=@ReminderId;
            """;
        var key = $"reminder:{candidate.ReminderId:N}:{candidate.ScheduledFor.UtcTicks}";
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            candidate.ClientId, candidate.UserId, candidate.HabitId, candidate.ReminderId,
            candidate.DispatchId, candidate.ScheduledFor, Next = nextOccurrence.UtcDateTime,
            Now = now.UtcDateTime, Title = "Hora do seu hábito",
            Message = $"Seu lembrete de {candidate.HabitName} está pronto.",
            ActionUrl = $"/habits/{candidate.HabitId}", DeduplicationKey = key
        }, transaction, cancellationToken: ct));
        transaction.Commit();
    }

    public async Task<bool> FailAsync(ReminderDispatchCandidate candidate, string errorCode,
        DateTimeOffset now, DateTimeOffset? nextAttempt, DateTimeOffset? nextOccurrence, CancellationToken ct = default)
    {
        using var connection = await connections.OpenAsync(ct);
        const string sql = """
            update habitflow.reminder_dispatches set status=@status,attempt_count=attempt_count+1,
                   error_code=@errorCode,last_error_at=@now,next_attempt_at=@nextAttempt,
                   locked_by=null,locked_until=null,processed_at=case when @nextAttempt is null then @now else null end
             where id=@DispatchId and client_id=@ClientId and user_id=@UserId
               and habit_id=@HabitId and habit_reminder_id=@ReminderId;
            update habitflow.habit_reminders set locked_by=null,locked_until=null,
                   next_trigger_at=coalesce(@nextAttempt,@nextOccurrence,next_trigger_at),updated_at=@now
             where id=@ReminderId and client_id=@ClientId and user_id=@UserId and habit_id=@HabitId;
            """;
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            candidate.DispatchId, candidate.ClientId, candidate.UserId, candidate.HabitId,
            candidate.ReminderId, errorCode, now = now.UtcDateTime,
            nextAttempt = nextAttempt?.UtcDateTime, nextOccurrence = nextOccurrence?.UtcDateTime, status = nextAttempt is null ? "Failed" : "Retry"
        }, cancellationToken: ct));
        return nextAttempt is not null;
    }

    public async Task<ReminderDispatchHealth> GetHealthAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        using var connection = await connections.OpenAsync(ct);
        const string sql = """
            select (select count(*) from habitflow.habit_reminders where is_active and next_trigger_at<=@now) "Due",
                   count(*) filter(where status in ('Pending','Processing')) "Pending",
                   count(*) filter(where status='Retry') "Retries",
                   count(*) filter(where status='Failed') "Failed"
              from habitflow.reminder_dispatches
            """;
        return await connection.QuerySingleAsync<ReminderDispatchHealth>(
            new CommandDefinition(sql, new { now = now.UtcDateTime }, cancellationToken: ct));
    }

    private sealed class ReminderDispatchCandidateRow
    {
        public Guid DispatchId { get; init; }
        public Guid ReminderId { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public string HabitName { get; init; } = "";
        public TimeOnly ReminderTime { get; init; }
        public string Timezone { get; init; } = "UTC";
        public int[] DaysOfWeek { get; init; } = [];
        public DateTime ScheduledFor { get; init; }
        public int AttemptCount { get; init; }
        public Guid CorrelationId { get; init; }

        public ReminderDispatchCandidate ToDomain() => new(
            DispatchId, ReminderId, ClientId, UserId, HabitId, HabitName, ReminderTime, Timezone,
            DaysOfWeek, new DateTimeOffset(DateTime.SpecifyKind(ScheduledFor, DateTimeKind.Utc)),
            AttemptCount, CorrelationId);
    }
}
