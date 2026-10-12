using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class RoutineSessionRepository(SqlExecutor db) : IRoutineSessionRepository
{
    private const string SessionColumns = """
        id, client_id, user_id, name, source_routine_id, source_routine_type, status, local_date,
        available_minutes, active_account_session_id, reflection, closure_summary, idempotency_key,
        version, created_at, updated_at, started_at, paused_at, resumed_at, closed_at, canceled_at
        """;

    private const string ItemColumns = """
        id, session_id, habit_id, habit_name_snapshot, habit_category_snapshot, sort_order, status,
        planned_quantity, planned_unit, minimum_quantity, minimum_name, recorded_quantity, recorded_unit,
        estimated_minutes, effective_minutes, skipped, skip_reason, completion_id, completed_at, updated_at
        """;

    public Task<RoutineSession?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, string idempotencyKey, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<RoutineSession>($"select {SessionColumns} from habitflow.routine_sessions where client_id=@clientId and user_id=@userId and idempotency_key=@idempotencyKey", new { clientId, userId, idempotencyKey }, ct);

    public async Task<RoutineSessionDetails?> GetAsync(Guid clientId, Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.QuerySingleOrDefaultAsync<RoutineSession>($"select {SessionColumns} from habitflow.routine_sessions where id=@sessionId and client_id=@clientId and user_id=@userId", new { clientId, userId, sessionId }, ct);
        if (session is null) return null;
        var items = (await db.QueryAsync<RoutineSessionItem>($"select {ItemColumns} from habitflow.routine_session_items where session_id=@sessionId order by sort_order, created_at, id", new { sessionId }, ct)).ToList();
        return new(session, items);
    }

    public async Task<RoutineSessionDetails?> GetActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var session = await db.QuerySingleOrDefaultAsync<RoutineSession>($"""
            select {SessionColumns}
              from habitflow.routine_sessions
             where client_id=@clientId and user_id=@userId
               and status in ('Prepared','InProgress','Paused')
             order by updated_at desc
             limit 1
            """, new { clientId, userId }, ct);
        return session is null ? null : await GetAsync(clientId, userId, session.Id, ct);
    }

    public async Task<IReadOnlyList<RoutineSessionSummary>> ListHistoryAsync(Guid clientId, Guid userId, DateOnly? from, DateOnly? to, string? status, int offset, int limit, CancellationToken ct = default) =>
        (await db.QueryAsync<RoutineSessionSummary>("""
            select s.id, s.name, s.status, s.local_date,
                   count(i.id)::int as total_items,
                   count(*) filter (where i.status='Completed')::int as completed_items,
                   count(*) filter (where i.status='MinimumReached')::int as minimum_items,
                   count(*) filter (where i.status='Skipped')::int as skipped_items,
                   count(*) filter (where i.status='Pending')::int as pending_items,
                   s.available_minutes,
                   nullif(sum(coalesce(i.effective_minutes,0)),0)::int as effective_minutes,
                   s.updated_at
              from habitflow.routine_sessions s
              left join habitflow.routine_session_items i on i.session_id=s.id
             where s.client_id=@clientId and s.user_id=@userId
               and (@from::date is null or s.local_date >= @from)
               and (@to::date is null or s.local_date <= @to)
               and (@status::text is null or s.status = @status)
             group by s.id
             order by s.local_date desc, s.updated_at desc
             offset @offset limit @limit
            """, new { clientId, userId, from, to, status = string.IsNullOrWhiteSpace(status) ? null : status, offset, limit }, ct)).ToList();

    public async Task CreateAsync(RoutineSession session, IReadOnlyList<RoutineSessionItem> items, string correlationId, CancellationToken ct = default)
    {
        await db.ExecuteAsync("""
            insert into habitflow.routine_sessions(
                id,client_id,user_id,name,source_routine_id,source_routine_type,status,local_date,available_minutes,
                active_account_session_id,reflection,closure_summary,idempotency_key,version,created_at,updated_at,
                started_at,paused_at,resumed_at,closed_at,canceled_at)
            values(@Id,@ClientId,@UserId,@Name,@SourceRoutineId,@SourceRoutineType,@Status,@LocalDate,@AvailableMinutes,
                @ActiveAccountSessionId,@Reflection,@ClosureSummary,@IdempotencyKey,@Version,@CreatedAt,@UpdatedAt,
                @StartedAt,@PausedAt,@ResumedAt,@ClosedAt,@CanceledAt)
            """, session, ct);
        foreach (var item in items)
        {
            await db.ExecuteAsync("""
                insert into habitflow.routine_session_items(
                    id,session_id,habit_id,habit_name_snapshot,habit_category_snapshot,sort_order,status,planned_quantity,
                    planned_unit,minimum_quantity,minimum_name,recorded_quantity,recorded_unit,estimated_minutes,effective_minutes,
                    skipped,skip_reason,completion_id,completed_at,updated_at)
                values(@Id,@SessionId,@HabitId,@HabitNameSnapshot,@HabitCategorySnapshot,@SortOrder,@Status,@PlannedQuantity,
                    @PlannedUnit,@MinimumQuantity,@MinimumName,@RecordedQuantity,@RecordedUnit,@EstimatedMinutes,@EffectiveMinutes,
                    @Skipped,@SkipReason,@CompletionId,@CompletedAt,@UpdatedAt)
                """, item, ct);
        }
        await db.ExecuteAsync("""
            insert into habitflow.routine_session_transitions(id,session_id,command,from_status,to_status,idempotency_key,correlation_id,created_at)
            values(gen_random_uuid(),@sessionId,'prepare','None','Prepared',@idempotencyKey,@correlationId,@createdAt)
            on conflict(session_id,idempotency_key) do nothing
            """, new { sessionId = session.Id, idempotencyKey = session.IdempotencyKey, correlationId, createdAt = session.CreatedAt }, ct);
    }

    public async Task<RoutineSession?> TransitionAsync(Guid clientId, Guid userId, Guid sessionId, string fromStatus, string toStatus, string command, string idempotencyKey, string correlationId, DateTime now, CancellationToken ct = default)
    {
        var inserted = await db.QuerySingleOrDefaultAsync<Guid?>("""
            insert into habitflow.routine_session_transitions(id,session_id,command,from_status,to_status,idempotency_key,correlation_id,created_at)
            select gen_random_uuid(), s.id, @command, @fromStatus, @toStatus, @idempotencyKey, @correlationId, @now
              from habitflow.routine_sessions s
             where s.id=@sessionId and s.client_id=@clientId and s.user_id=@userId and s.status=@fromStatus
            on conflict(session_id,idempotency_key) do nothing
            returning id
            """, new { clientId, userId, sessionId, fromStatus, toStatus, command, idempotencyKey, correlationId, now }, ct);
        if (!inserted.HasValue) return null;

        return await db.QuerySingleOrDefaultAsync<RoutineSession>($"""
            update habitflow.routine_sessions
               set status=@toStatus,
                   version=version+1,
                   updated_at=@now,
                   started_at=case when @command='start' then coalesce(started_at,@now) else started_at end,
                   paused_at=case when @command='pause' then @now else paused_at end,
                   resumed_at=case when @command='resume' then @now else resumed_at end,
                   closed_at=case when @toStatus='Closed' then coalesce(closed_at,@now) else closed_at end,
                   canceled_at=case when @toStatus='Canceled' then coalesce(canceled_at,@now) else canceled_at end
             where id=@sessionId and client_id=@clientId and user_id=@userId and status=@fromStatus
             returning {SessionColumns}
            """, new { clientId, userId, sessionId, fromStatus, toStatus, command, now }, ct);
    }

    public Task<RoutineSessionItem?> UpdateItemAsync(Guid clientId, Guid userId, Guid sessionId, Guid itemId, string status, decimal? recordedQuantity, string? recordedUnit, int? effectiveMinutes, Guid? completionId, bool skipped, string? skipReason, DateTime now, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<RoutineSessionItem>($"""
            update habitflow.routine_session_items i
               set status=@status,
                   recorded_quantity=coalesce(@recordedQuantity, recorded_quantity),
                   recorded_unit=coalesce(@recordedUnit, recorded_unit),
                   effective_minutes=coalesce(@effectiveMinutes, effective_minutes),
                   skipped=@skipped,
                   skip_reason=@skipReason,
                   completion_id=coalesce(@completionId, completion_id),
                   completed_at=case when @status='Completed' then coalesce(completed_at,@now) else completed_at end,
                   updated_at=@now
              from habitflow.routine_sessions s
             where s.id=i.session_id and s.id=@sessionId and s.client_id=@clientId and s.user_id=@userId and i.id=@itemId
             returning i.{ItemColumns.Replace(", ", ", i.")}
            """, new { clientId, userId, sessionId, itemId, status, recordedQuantity, recordedUnit, effectiveMinutes, completionId, skipped, skipReason, now }, ct);

    public Task SetClosureAsync(Guid clientId, Guid userId, Guid sessionId, string? reflection, string summary, DateTime now, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            update habitflow.routine_sessions
               set reflection=nullif(@reflection,''),
                   closure_summary=@summary,
                   updated_at=@now
             where id=@sessionId and client_id=@clientId and user_id=@userId and status='Closed'
            """, new { clientId, userId, sessionId, reflection = reflection?.Trim(), summary, now }, ct);
}
