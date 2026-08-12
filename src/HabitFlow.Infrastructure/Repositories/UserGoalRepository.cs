using HabitFlow.Domain;
namespace HabitFlow.Infrastructure;
public sealed class UserGoalRepository(SqlExecutor db) : IUserGoalRepository
{
 const string Columns="id,client_id,user_id,objective_slug,title,description,target_type,target_value,current_value,start_date,end_date,status,color,icon,created_at,updated_at,completed_at";
 public async Task<IReadOnlyList<UserGoal>> ListAsync(Guid c,Guid u,CancellationToken ct=default)=>(await db.QueryAsync<UserGoal>($"select {Columns} from habitflow.user_goals where client_id=@c and user_id=@u order by status='Active' desc,created_at desc",new{c,u},ct)).ToList();
 public Task<UserGoal?> GetAsync(Guid id,Guid c,Guid u,CancellationToken ct=default)=>db.QuerySingleOrDefaultAsync<UserGoal>($"select {Columns} from habitflow.user_goals where id=@id and client_id=@c and user_id=@u",new{id,c,u},ct);
 public Task<int> CountActiveAsync(Guid c,Guid u,CancellationToken ct=default)=>db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.user_goals where client_id=@c and user_id=@u and status='Active'",new{c,u},ct)!;
 public Task CreateAsync(UserGoal g,CancellationToken ct=default)=>db.ExecuteAsync("insert into habitflow.user_goals(id,client_id,user_id,objective_slug,title,description,target_type,target_value,current_value,start_date,end_date,status,color,icon,created_at,updated_at,completed_at) values(@Id,@ClientId,@UserId,@ObjectiveSlug,@Title,@Description,@TargetType,@TargetValue,@CurrentValue,@StartDate,@EndDate,@Status,@Color,@Icon,@CreatedAt,@UpdatedAt,@CompletedAt)",g,ct);
 public Task UpdateAsync(UserGoal g,CancellationToken ct=default)=>db.ExecuteAsync("update habitflow.user_goals set title=@Title,description=@Description,target_type=@TargetType,target_value=@TargetValue,start_date=@StartDate,end_date=@EndDate,color=@Color,icon=@Icon,updated_at=@UpdatedAt where id=@Id and client_id=@ClientId and user_id=@UserId",g,ct);
 public Task SetStatusAsync(Guid id,Guid c,Guid u,string status,CancellationToken ct=default)=>db.ExecuteAsync("update habitflow.user_goals set status=@status,updated_at=now(),completed_at=case when @status='Completed' then now() else completed_at end where id=@id and client_id=@c and user_id=@u",new{id,c,u,status},ct);
 public Task LinkHabitAsync(Guid goalId,Guid habitId,Guid c,Guid u,CancellationToken ct=default)=>db.ExecuteAsync("insert into habitflow.goal_habits(goal_id,habit_id,client_id) select g.id,h.id,g.client_id from habitflow.user_goals g join habitflow.habits h on h.id=@habitId and h.user_id=g.user_id and h.client_id=g.client_id where g.id=@goalId and g.client_id=@c and g.user_id=@u on conflict do nothing",new{goalId,habitId,c,u},ct);
 public Task UnlinkHabitAsync(Guid goalId,Guid habitId,Guid c,Guid u,CancellationToken ct=default)=>db.ExecuteAsync("delete from habitflow.goal_habits gh using habitflow.user_goals g where gh.goal_id=g.id and gh.goal_id=@goalId and gh.habit_id=@habitId and g.client_id=@c and g.user_id=@u",new{goalId,habitId,c,u},ct);
 public async Task<IReadOnlyList<Habit>> ListLinkedHabitsAsync(Guid goalId,Guid c,Guid u,CancellationToken ct=default)=>(await db.QueryAsync<Habit>($"""
  select {HabitSql.AliasedColumns} from habitflow.habits h
  join habitflow.goal_habits gh on gh.habit_id=h.id and gh.client_id=h.client_id
  join habitflow.user_goals g on g.id=gh.goal_id and g.client_id=h.client_id and g.user_id=h.user_id
  where g.id=@goalId and g.client_id=@c and g.user_id=@u order by h.is_archived,h.sort_order,h.name
  """,new{goalId,c,u},ct)).ToList();
 public async Task<IReadOnlyList<GoalTimelineEntry>> ListTimelineAsync(Guid goalId,Guid c,Guid u,CancellationToken ct=default)=>(await db.QueryAsync<GoalTimelineEntry>("""
  select e.event_type,e.previous_value,e.new_value current_value,e.local_date,e.created_at
  from habitflow.goal_progress_events e join habitflow.user_goals g on g.id=e.goal_id
  where e.goal_id=@goalId and e.client_id=@c and e.user_id=@u and g.client_id=@c and g.user_id=@u
  order by e.created_at desc limit 30
  """,new{goalId,c,u},ct)).ToList();
}
