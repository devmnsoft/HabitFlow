using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class GoalProgressEventRepository(SqlExecutor db) : IGoalProgressRepository
{
    private const string Columns = "g.id,g.client_id,g.user_id,g.objective_slug,g.title,g.description,g.target_type,g.target_value,g.current_value,g.start_date,g.end_date,g.status,g.color,g.icon,g.created_at,g.updated_at,g.completed_at";

    public async Task<IReadOnlyList<UserGoal>> ListRelatedAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserGoal>($"""
            select {Columns}
              from habitflow.user_goals g
             where g.client_id=@clientId and g.user_id=@userId
               and g.status in ('Active','Completed')
               and (not exists (select 1 from habitflow.goal_habits x where x.goal_id=g.id)
                    or exists (select 1 from habitflow.goal_habits x where x.goal_id=g.id and x.client_id=@clientId and x.habit_id=@habitId))
            """, new { clientId, userId, habitId }, ct)).ToList();

    public async Task<GoalProgressSnapshot> BuildSnapshotAsync(UserGoal goal, Guid triggerHabitId, DateOnly localDate, int currentStreak, CancellationToken ct = default)
    {
        var weekStart = localDate.AddDays(-(((int)localDate.DayOfWeek + 6) % 7));
        var values = await db.QuerySingleOrDefaultAsync<AggregateValues>("""
            with valid as (
              select distinct c.id,c.completed_date
                from habitflow.habit_completions c
                join habitflow.habits h on h.id=c.habit_id and h.client_id=@ClientId and h.user_id=@UserId
               where c.client_id=@ClientId and c.user_id=@UserId
                 and c.completed_date between @StartDate and coalesce(@EndDate,@LocalDate)
                 and (not exists(select 1 from habitflow.goal_habits gh where gh.goal_id=@Id)
                      or exists(select 1 from habitflow.goal_habits gh where gh.goal_id=@Id and gh.client_id=@ClientId and gh.habit_id=c.habit_id))
            )
            select count(*)::int habit_completions,
                   count(distinct completed_date)::int active_days,
                   count(*) filter(where completed_date between @weekStart and @LocalDate)::int weekly_completions
              from valid
            """, new { goal.Id, goal.ClientId, goal.UserId, goal.StartDate, goal.EndDate, LocalDate = localDate, weekStart }, ct) ?? new();
        return new(values.HabitCompletions, values.ActiveDays, currentStreak, values.WeeklyCompletions);
    }

    public async Task<bool> ApplyAsync(UserGoal goal, GoalProgressResult result, GoalProgressEvent e, CancellationToken ct = default)
    {
        var inserted = await db.QuerySingleOrDefaultAsync<Guid?>("""
            insert into habitflow.goal_progress_events
              (id,client_id,user_id,goal_id,event_type,previous_value,new_value,local_date,source_completion_id,idempotency_key,correlation_id,created_at,metadata_json)
            values (@Id,@ClientId,@UserId,@GoalId,@EventType,@PreviousValue,@NewValue,@LocalDate,@SourceCompletionId,@IdempotencyKey,@CorrelationId,@CreatedAtUtc,cast(@MetadataJson as jsonb))
            on conflict(idempotency_key) do nothing returning id
            """, e, ct);
        if (!inserted.HasValue) return false;
        await db.ExecuteAsync("""
            update habitflow.user_goals set current_value=@CurrentValue,status=@Status,
              completed_at=coalesce(completed_at,@CompletedAtUtc),updated_at=now()
             where id=@GoalId and client_id=@ClientId and user_id=@UserId
            """, new { result.CurrentValue, result.Status, result.CompletedAtUtc, result.GoalId, goal.ClientId, goal.UserId }, ct);
        return true;
    }

    private sealed class AggregateValues
    {
        public int HabitCompletions { get; init; }
        public int ActiveDays { get; init; }
        public int WeeklyCompletions { get; init; }
    }
}
