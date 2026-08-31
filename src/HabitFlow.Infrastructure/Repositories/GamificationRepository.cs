using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class GamificationRepository(SqlExecutor db) : IGamificationRepository
{
    private const int DailyPointsLimit = 100;
    public async Task<WeeklyGoal?> CreateWeeklyGoalAsync(WeeklyGoal goal, IReadOnlyCollection<Guid> habitIds, CancellationToken ct = default)
    {
        const string createGoalSql = """
            insert into habitflow.weekly_goals
                (id, client_id, user_id, name, week_start, week_end, target_completions, current_completions, status, created_at)
            select
                @Id, @ClientId, @UserId, @Name, @WeekStart, @WeekEnd, @TargetCompletions, 0, 'Active', @CreatedAt
            where exists (
                select 1
                from habitflow.users
                where id = @UserId and client_id = @ClientId
            )
            on conflict (client_id, user_id, week_start, name) do nothing
            returning *
            """;

        var created = await db.QuerySingleOrDefaultAsync<WeeklyGoal>(createGoalSql, goal, ct);
        if (created is null)
            return null;

        const string linkHabitSql = """
            insert into habitflow.weekly_goal_habits (client_id, user_id, weekly_goal_id, habit_id)
            select @ClientId, @UserId, @GoalId, h.id
            from habitflow.habits h
            where h.id = @HabitId
              and h.client_id = @ClientId
              and h.user_id = @UserId
              and not h.is_archived
            on conflict do nothing
            """;

        foreach (var habitId in habitIds.Distinct())
        {
            await db.ExecuteAsync(linkHabitSql, new
            {
                goal.ClientId,
                goal.UserId,
                GoalId = goal.Id,
                HabitId = habitId
            }, ct);
        }

        return created;
    }

    public async Task<IReadOnlyList<WeeklyGoal>> ListWeeklyGoalsAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select *
            from habitflow.weekly_goals
            where client_id = @clientId and user_id = @userId
            order by week_start desc
            """;

        return (await db.QueryAsync<WeeklyGoal>(sql, new { clientId, userId }, ct)).ToList();
    }

    public async Task<IReadOnlyList<UserAchievement>> ListAchievementsAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select ua.*, d.name, d.description, d.icon, d.category, d.rarity
            from habitflow.user_achievements ua
            join habitflow.achievement_definitions d on d.code = ua.achievement_code
            where ua.client_id = @clientId and ua.user_id = @userId
            order by ua.unlocked_at desc
            """;

        return (await db.QueryAsync<UserAchievement>(sql, new { clientId, userId }, ct)).ToList();
    }

    public async Task<IReadOnlyList<AchievementDefinition>> ListLockedDefinitionsAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select d.*
            from habitflow.achievement_definitions d
            where d.is_active
              and not exists (
                  select 1
                  from habitflow.user_achievements ua
                  where ua.client_id = @clientId
                    and ua.user_id = @userId
                    and ua.achievement_code = d.code
              )
            order by case d.rarity
                when 'comum' then 1
                when 'especial' then 2
                else 3
            end, d.code
            limit 4
            """;

        return (await db.QueryAsync<AchievementDefinition>(sql, new { clientId, userId }, ct)).ToList();
    }

    public async Task<IReadOnlyList<WeeklyGoal>> ApplyCompletionAsync(Guid clientId, Guid userId, Guid habitId, Guid completionId, DateOnly date, CancellationToken ct = default)
    {
        const string sql = """
            with accepted as (
                insert into habitflow.gamification_events
                    (id, client_id, user_id, event_type, entity_type, entity_id, idempotency_key)
                values
                    (gen_random_uuid(), @clientId, @userId, 'weekly_goal.progress', 'completion', @completionId, 'weekly-goal:' || @completionId)
                on conflict (client_id, user_id, idempotency_key) do nothing
                returning id
            )
            update habitflow.weekly_goals g
            set current_completions = least(target_completions, current_completions + 1),
                status = case
                    when current_completions + 1 >= target_completions then 'Completed'
                    else status
                end,
                completed_at = case
                    when current_completions + 1 >= target_completions then coalesce(completed_at, now())
                    else completed_at
                end
            where g.client_id = @clientId
              and g.user_id = @userId
              and g.status = 'Active'
              and @date between g.week_start and g.week_end
              and exists (
                  select 1
                  from habitflow.weekly_goal_habits l
                  join habitflow.habits h on h.id = l.habit_id and not h.is_archived
                  where l.client_id = @clientId
                    and l.user_id = @userId
                    and l.weekly_goal_id = g.id
                    and l.habit_id = @habitId
              )
              and exists (select 1 from accepted)
            returning g.*
            """;

        return (await db.QueryAsync<WeeklyGoal>(sql, new
        {
            clientId,
            userId,
            habitId,
            completionId,
            date
        }, ct)).ToList();
    }

    public Task<int> CountCompletionsAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select count(*)
            from habitflow.habit_completions
            where client_id = @clientId and user_id = @userId
            """;

        return db.QuerySingleOrDefaultAsync<int>(sql, new { clientId, userId }, ct)!;
    }

    public async Task<bool> UnlockAsync(Guid clientId, Guid userId, string code, DateTime unlockedAt, CancellationToken ct = default)
    {
        const string sql = """
            with unlocked as (
                insert into habitflow.user_achievements
                    (id, client_id, user_id, achievement_code, unlocked_at)
                select gen_random_uuid(), @clientId, @userId, d.code, @unlockedAt
                from habitflow.achievement_definitions d
                where d.code = @code and d.is_active
                on conflict (client_id, user_id, achievement_code) do nothing
                returning id
            )
            insert into habitflow.gamification_events
                (id, client_id, user_id, event_type, entity_type, entity_id, idempotency_key)
            select gen_random_uuid(), @clientId, @userId, 'achievement.unlocked', 'achievement', id, 'achievement:' || @code
            from unlocked
            on conflict (client_id, user_id, idempotency_key) do nothing
            returning entity_id
            """;

        var achievementId = await db.QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            clientId,
            userId,
            code,
            unlockedAt
        }, ct);

        return achievementId.HasValue;
    }

    public async Task<bool> UseFreezeAsync(StreakFreeze freeze, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.streak_freezes
                (id, client_id, user_id, habit_id, frozen_date, reason, created_at)
            select @Id, @ClientId, @UserId, h.id, @FrozenDate, @Reason, @CreatedAt
            from habitflow.habits h
            where h.id = @HabitId
              and h.client_id = @ClientId
              and h.user_id = @UserId
            on conflict (client_id, user_id, habit_id, frozen_date) do nothing
            """;

        return await db.ExecuteAsync(sql, freeze, ct) == 1;
    }

    public async Task<int> GrantPointsAsync(Guid clientId, Guid userId, Guid completionId, int points, DateOnly localDate, DateTime occurredAt, CancellationToken ct = default)
    {
        const string sql = """
            with used as (select coalesce(sum(points),0)::int value from habitflow.gamification_points_ledger
              where client_id=@clientId and user_id=@userId and local_date=@localDate),
            added as (insert into habitflow.gamification_points_ledger(id,client_id,user_id,source_type,source_id,points,local_date,occurred_at,idempotency_key)
              select gen_random_uuid(),@clientId,@userId,'completion',@completionId,least(@points,greatest(0,@limit-used.value)),@localDate,@occurredAt,'completion:'||@completionId
              from used where used.value < @limit on conflict(client_id,user_id,idempotency_key) do nothing returning points)
            select coalesce((select points from added),0)
            """;
        return await db.QuerySingleOrDefaultAsync<int>(sql,new {clientId,userId,completionId,points=Math.Max(0,points),localDate,occurredAt,limit=DailyPointsLimit},ct);
    }

    public async Task<int> RevertPointsAsync(Guid clientId, Guid userId, Guid completionId, DateTime occurredAt, CancellationToken ct = default)
    {
        const string sql = """
            with original as (select coalesce(sum(points),0)::int points,min(local_date) local_date from habitflow.gamification_points_ledger
              where client_id=@clientId and user_id=@userId and source_id=@completionId and source_type='completion'),
            reverted as (insert into habitflow.gamification_points_ledger(id,client_id,user_id,source_type,source_id,points,local_date,occurred_at,idempotency_key)
              select gen_random_uuid(),@clientId,@userId,'reversal',@completionId,-points,local_date,@occurredAt,'reversal:'||@completionId from original where points>0
              on conflict(client_id,user_id,idempotency_key) do nothing returning points) select coalesce((select points from reverted),0)
            """;
        return await db.QuerySingleOrDefaultAsync<int>(sql,new {clientId,userId,completionId,occurredAt},ct);
    }

    public async Task<PointsBalance> GetPointsAsync(Guid clientId, Guid userId, DateOnly localDate, CancellationToken ct=default)
    {
        const string sql="select coalesce(sum(points),0)::int total_points,coalesce(sum(points) filter(where local_date=@localDate),0)::int today_points,@limit daily_limit from habitflow.gamification_points_ledger where client_id=@clientId and user_id=@userId";
        return await db.QuerySingleOrDefaultAsync<PointsBalance>(sql,new{clientId,userId,localDate,limit=DailyPointsLimit},ct) ?? new(0,0,DailyPointsLimit);
    }
    public Task<LeaderboardPreference?> GetLeaderboardPreferenceAsync(Guid clientId,Guid userId,CancellationToken ct=default) => db.QuerySingleOrDefaultAsync<LeaderboardPreference>("select client_id,user_id,is_opted_in,scope,public_name,team_id,updated_at from habitflow.gamification_leaderboard_preferences where client_id=@clientId and user_id=@userId",new{clientId,userId},ct);
    public Task SaveLeaderboardPreferenceAsync(LeaderboardPreference p,CancellationToken ct=default) => db.ExecuteAsync("insert into habitflow.gamification_leaderboard_preferences(client_id,user_id,is_opted_in,scope,public_name,team_id,updated_at) values(@ClientId,@UserId,@IsOptedIn,@Scope,@PublicName,@TeamId,@UpdatedAt) on conflict(client_id,user_id) do update set is_opted_in=excluded.is_opted_in,scope=excluded.scope,public_name=excluded.public_name,team_id=excluded.team_id,updated_at=excluded.updated_at",new{p.ClientId,p.UserId,p.IsOptedIn,Scope=p.Scope.ToString(),p.PublicName,p.TeamId,p.UpdatedAt},ct);
    public async Task<IReadOnlyList<LeaderboardEntry>> ListLeaderboardAsync(Guid clientId,Guid userId,LeaderboardScope scope,CancellationToken ct=default)
    {
        const string sql="""select row_number() over(order by sum(l.points) desc,p.public_name)::int position,p.public_name,sum(l.points)::int points,p.scope from habitflow.gamification_leaderboard_preferences p join habitflow.gamification_points_ledger l on l.client_id=p.client_id and l.user_id=p.user_id where p.client_id=@clientId and p.is_opted_in and p.scope=@scope and (@scope<>'Team' or p.team_id=(select team_id from habitflow.gamification_leaderboard_preferences where client_id=@clientId and user_id=@userId)) group by p.user_id,p.public_name,p.scope order by points desc limit 100""";
        return (await db.QueryAsync<LeaderboardEntry>(sql,new{clientId,userId,scope=scope.ToString()},ct)).ToList();
    }
}
