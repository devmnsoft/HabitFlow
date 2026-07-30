using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class MilestoneRepository(SqlExecutor db) : IMilestoneRepository
{
    public async Task<IReadOnlyList<MilestoneEvaluationResult>> AwardEligibleAsync(MilestoneEvaluationContext context, CancellationToken ct = default) =>
        (await db.QueryAsync<MilestoneEvaluationResult>("""
            with metrics as (
              select count(distinct c.completed_date)::int active_days,
                     count(*)::int completions
                from habitflow.habit_completions c
               where c.client_id=@ClientId and c.user_id=@UserId
            ), eligible as (
              select m.id,m.code,m.title,m.description
                from habitflow.milestones m cross join metrics x
               where m.is_active
                 and ((m.code='first_step' and x.completions >= 1)
                   or (m.code='present_3' and x.active_days >= 3)
                   or (m.code='rhythm_7' and x.active_days >= 7)
                   or (m.code='consistency_15' and x.active_days >= 15)
                   or (m.code='evolution_30' and x.active_days >= 30)
                   or (m.code='first_goal_completed' and @GoalCompletedNow))
            ), awarded as (
              insert into habitflow.user_milestones(id,client_id,user_id,milestone_id,achieved_at,metadata)
              select gen_random_uuid(),@ClientId,@UserId,e.id,now(),
                     jsonb_build_object('localDate',@LocalDate::text,'correlationId',@CorrelationId)
                from eligible e
              on conflict(user_id,milestone_id) do nothing
              returning milestone_id,achieved_at
            )
            select e.id milestone_id,e.code,e.title,e.description message,a.achieved_at achieved_at_utc
              from awarded a join eligible e on e.id=a.milestone_id
            """, context, ct)).ToList();
}
