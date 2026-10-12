using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class PersonalPlanningRepository(SqlExecutor db) : IPersonalPlanningRepository
{
    public async Task<IReadOnlyList<PersonalPriorityDetails>> ListPrioritiesAsync(Guid clientId, Guid userId, DateOnly onDate, CancellationToken ct = default)
    {
        var priorities = (await db.QueryAsync<PersonalPriority>("""
            select id, client_id as ClientId, user_id as UserId, goal_id as GoalId, priority_order as PriorityOrder,
                   weekly_available_minutes as WeeklyAvailableMinutes, effective_from as EffectiveFrom,
                   effective_until as EffectiveUntil, version, created_at as CreatedAt, updated_at as UpdatedAt
            from habitflow.personal_priorities
            where client_id=@clientId and user_id=@userId
              and effective_from <= @onDate and (effective_until is null or effective_until >= @onDate)
            order by priority_order, created_at
            """, new { clientId, userId, onDate }, ct)).ToList();

        var result = new List<PersonalPriorityDetails>();
        foreach (var priority in priorities)
        {
            var goal = await db.QuerySingleOrDefaultAsync<UserGoal>("""
                select id,client_id,user_id,objective_slug,title,description,target_type,target_value,current_value,
                       start_date,end_date,status,color,icon,created_at,updated_at,completed_at
                from habitflow.user_goals
                where id=@goalId and client_id=@clientId and user_id=@userId
                """, new { goalId = priority.GoalId, clientId, userId }, ct);
            if (goal is null) continue;
            var habits = (await db.QueryAsync<Habit>($"""
                select {HabitSql.AliasedColumns}
                from habitflow.goal_habits gh
                join habitflow.habits h on h.id=gh.habit_id and h.client_id=@clientId and h.user_id=@userId
                where gh.goal_id=@goalId and gh.client_id=@clientId
                order by h.sort_order, h.name
                """, new { goalId = priority.GoalId, clientId, userId }, ct)).ToList();
            result.Add(new(priority, goal, habits));
        }

        return result;
    }

    public async Task SavePrioritiesAsync(Guid clientId, Guid userId, IReadOnlyList<PersonalPriority> priorities, CancellationToken ct = default)
    {
        var today = priorities.FirstOrDefault()?.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
        await db.ExecuteAsync("""
            update habitflow.personal_priorities
            set effective_until=@yesterday, updated_at=now(), version=version+1
            where client_id=@clientId and user_id=@userId and effective_until is null
              and effective_from < @today
            """, new { clientId, userId, today, yesterday = today.AddDays(-1) }, ct);
        await db.ExecuteAsync("""
            delete from habitflow.personal_priorities
            where client_id=@clientId and user_id=@userId and effective_from=@today
            """, new { clientId, userId, today }, ct);
        foreach (var priority in priorities)
        {
            await db.ExecuteAsync("""
                insert into habitflow.personal_priorities(id,client_id,user_id,goal_id,priority_order,weekly_available_minutes,effective_from,effective_until,version,created_at,updated_at)
                select @Id,@ClientId,@UserId,g.id,@PriorityOrder,@WeeklyAvailableMinutes,@EffectiveFrom,@EffectiveUntil,@Version,@CreatedAt,@UpdatedAt
                from habitflow.user_goals g
                where g.id=@GoalId and g.client_id=@ClientId and g.user_id=@UserId and g.status='Active'
                on conflict(client_id,user_id,goal_id,effective_from) do update
                set priority_order=excluded.priority_order,
                    weekly_available_minutes=excluded.weekly_available_minutes,
                    effective_until=excluded.effective_until,
                    version=habitflow.personal_priorities.version+1,
                    updated_at=excluded.updated_at
                """, priority, ct);
        }
    }

    public async Task<IReadOnlySet<string>> ListDismissedSuggestionKeysAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default) =>
        (await db.QueryAsync<string>("""
            select suggestion_key
            from habitflow.dismissed_next_steps
            where client_id=@clientId and user_id=@userId and dismissed_for_date=@date
            """, new { clientId, userId, date }, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public Task DismissSuggestionAsync(Guid clientId, Guid userId, string suggestionKey, DateOnly date, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.dismissed_next_steps(id,client_id,user_id,suggestion_key,dismissed_for_date,created_at)
            values(@id,@clientId,@userId,@suggestionKey,@date,now())
            on conflict(client_id,user_id,suggestion_key,dismissed_for_date) do nothing
            """, new { id = Guid.NewGuid(), clientId, userId, suggestionKey, date }, ct);

    public Task<MonthlyReview?> GetMonthlyReviewAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<MonthlyReview>("""
            select id, client_id as ClientId, user_id as UserId, period_start as PeriodStart, period_end as PeriodEnd,
                   status, reflection, selected_next_step as SelectedNextStep, confirmed_plan_code as ConfirmedPlanCode,
                   idempotency_key as IdempotencyKey, version, created_at as CreatedAt, updated_at as UpdatedAt, completed_at as CompletedAt
            from habitflow.monthly_reviews
            where client_id=@clientId and user_id=@userId and period_start=@periodStart
            """, new { clientId, userId, periodStart }, ct);

    public async Task<MonthlyReview> UpsertMonthlyReviewAsync(MonthlyReview review, int expectedVersion, CancellationToken ct = default)
    {
        await db.ExecuteAsync("""
            insert into habitflow.monthly_reviews(id,client_id,user_id,period_start,period_end,status,reflection,selected_next_step,confirmed_plan_code,idempotency_key,version,created_at,updated_at,completed_at)
            values(@Id,@ClientId,@UserId,@PeriodStart,@PeriodEnd,@Status,@Reflection,@SelectedNextStep,@ConfirmedPlanCode,@IdempotencyKey,1,@CreatedAt,@UpdatedAt,@CompletedAt)
            on conflict(client_id,user_id,period_start) do update
            set status=excluded.status,
                reflection=excluded.reflection,
                selected_next_step=excluded.selected_next_step,
                confirmed_plan_code=excluded.confirmed_plan_code,
                completed_at=excluded.completed_at,
                version=habitflow.monthly_reviews.version+1,
                updated_at=excluded.updated_at
            where habitflow.monthly_reviews.version=@expectedVersion
               or habitflow.monthly_reviews.status='Draft'
            """, review with { Version = expectedVersion }, ct);
        return (await GetMonthlyReviewAsync(review.ClientId, review.UserId, review.PeriodStart, ct))!;
    }
}
