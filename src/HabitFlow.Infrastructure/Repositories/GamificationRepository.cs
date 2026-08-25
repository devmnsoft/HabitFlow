using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class GamificationRepository(SqlExecutor db) : IGamificationRepository
{
    public async Task<WeeklyGoal?> CreateWeeklyGoalAsync(WeeklyGoal g, IReadOnlyCollection<Guid> habitIds, CancellationToken ct = default)
    {
        var created = await db.QuerySingleOrDefaultAsync<WeeklyGoal>("""
            insert into habitflow.weekly_goals(id,client_id,user_id,name,week_start,week_end,target_completions,current_completions,status,created_at)
            select @Id,@ClientId,@UserId,@Name,@WeekStart,@WeekEnd,@TargetCompletions,0,'Active',@CreatedAt
            where exists(select 1 from habitflow.users where id=@UserId and client_id=@ClientId)
            on conflict(client_id,user_id,week_start,name) do nothing returning *
            """, g, ct);
        if (created is null) return null;
        foreach (var habitId in habitIds.Distinct())
            await db.ExecuteAsync("""insert into habitflow.weekly_goal_habits(client_id,user_id,weekly_goal_id,habit_id)
                select @ClientId,@UserId,@GoalId,h.id from habitflow.habits h where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId and not h.is_archived
                on conflict do nothing""", new { g.ClientId, g.UserId, GoalId = g.Id, HabitId = habitId }, ct);
        return created;
    }
    public async Task<IReadOnlyList<WeeklyGoal>> ListWeeklyGoalsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<WeeklyGoal>("select * from habitflow.weekly_goals where client_id=@clientId and user_id=@userId order by week_start desc", new {clientId,userId},ct)).ToList();
    public async Task<IReadOnlyList<UserAchievement>> ListAchievementsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserAchievement>("""select ua.*,d.name,d.description,d.icon,d.category,d.rarity from habitflow.user_achievements ua join habitflow.achievement_definitions d on d.code=ua.achievement_code where ua.client_id=@clientId and ua.user_id=@userId order by ua.unlocked_at desc""",new{clientId,userId},ct)).ToList();
    public async Task<IReadOnlyList<AchievementDefinition>> ListLockedDefinitionsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<AchievementDefinition>("""select d.* from habitflow.achievement_definitions d where d.is_active and not exists(select 1 from habitflow.user_achievements ua where ua.client_id=@clientId and ua.user_id=@userId and ua.achievement_code=d.code) order by case d.rarity when 'comum' then 1 when 'especial' then 2 else 3 end,d.code limit 4""",new{clientId,userId},ct)).ToList();
    public async Task<IReadOnlyList<WeeklyGoal>> ApplyCompletionAsync(Guid clientId, Guid userId, Guid habitId, Guid completionId, DateOnly date, CancellationToken ct = default)
    {
        return (await db.QueryAsync<WeeklyGoal>("""with accepted as (
            insert into habitflow.gamification_events(id,client_id,user_id,event_type,entity_type,entity_id,idempotency_key)
            values(gen_random_uuid(),@clientId,@userId,'weekly_goal.progress','completion',@completionId,'weekly-goal:'||@completionId)
            on conflict(client_id,user_id,idempotency_key) do nothing returning id)
            update habitflow.weekly_goals g set current_completions=least(target_completions,current_completions+1),
            status=case when current_completions+1>=target_completions then 'Completed' else status end,
            completed_at=case when current_completions+1>=target_completions then coalesce(completed_at,now()) else completed_at end
            where g.client_id=@clientId and g.user_id=@userId and g.status='Active' and @date between g.week_start and g.week_end
            and exists(select 1 from habitflow.weekly_goal_habits l join habitflow.habits h on h.id=l.habit_id and not h.is_archived where l.client_id=@clientId and l.user_id=@userId and l.weekly_goal_id=g.id and l.habit_id=@habitId)
            and exists(select 1 from accepted) returning g.*""",new{clientId,userId,habitId,completionId,date},ct)).ToList();
    }
    public Task<int> CountCompletionsAsync(Guid clientId, Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.habit_completions where client_id=@clientId and user_id=@userId",new{clientId,userId},ct)!;
    public async Task<bool> UnlockAsync(Guid clientId, Guid userId, string code, DateTime unlockedAt, CancellationToken ct = default)
    {
        var achievementId = await db.QuerySingleOrDefaultAsync<Guid?>("""with unlocked as (
            insert into habitflow.user_achievements(id,client_id,user_id,achievement_code,unlocked_at)
            select gen_random_uuid(),@clientId,@userId,d.code,@unlockedAt from habitflow.achievement_definitions d where d.code=@code and d.is_active
            on conflict(client_id,user_id,achievement_code) do nothing returning id)
            insert into habitflow.gamification_events(id,client_id,user_id,event_type,entity_type,entity_id,idempotency_key)
            select gen_random_uuid(),@clientId,@userId,'achievement.unlocked','achievement',id,'achievement:'||@code from unlocked
            on conflict(client_id,user_id,idempotency_key) do nothing returning entity_id""",new{clientId,userId,code,unlockedAt},ct);
        return achievementId.HasValue;
    }
    public async Task<bool> UseFreezeAsync(StreakFreeze f, CancellationToken ct = default) =>
        await db.ExecuteAsync("""insert into habitflow.streak_freezes(id,client_id,user_id,habit_id,frozen_date,reason,created_at)
            select @Id,@ClientId,@UserId,h.id,@FrozenDate,@Reason,@CreatedAt from habitflow.habits h where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId
            on conflict(client_id,user_id,habit_id,frozen_date) do nothing""",f,ct)==1;
}
