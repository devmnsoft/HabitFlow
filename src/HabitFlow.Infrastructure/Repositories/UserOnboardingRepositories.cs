using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserOnboardingProgressRepository(SqlExecutor db) : IUserOnboardingProgressRepository
{
    private const string Columns = "client_id,user_id,current_step,selected_objective_slug,available_minutes,preferred_frequency,preferred_days,preferred_time,selected_template_ids,selected_collection_id,create_goal,goal_target_type,goal_target_value,started_at,last_activity_at,completed_at,skipped_at,version";
    public Task<UserOnboardingProgress?> GetAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<UserOnboardingProgress>($"select {Columns} from habitflow.user_onboarding_progress where client_id=@clientId and user_id=@userId", new { clientId, userId }, ct);

    public async Task<UserOnboardingProgress> StartOrRestartAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        await db.ExecuteAsync("""
          insert into habitflow.user_onboarding_progress(client_id,user_id,current_step) values(@clientId,@userId,1)
          on conflict(client_id,user_id) do update set current_step=1,selected_objective_slug=null,available_minutes=null,
          preferred_frequency=null,preferred_days='{}',preferred_time=null,selected_template_ids='{}',selected_collection_id=null,
          create_goal=false,goal_target_type=null,goal_target_value=null,started_at=now(),last_activity_at=now(),completed_at=null,
          skipped_at=null,version=habitflow.user_onboarding_progress.version+1
          """, new { clientId, userId }, ct);
        return (await GetAsync(clientId, userId, ct))!;
    }

    public async Task<bool> SaveAsync(UserOnboardingProgress p, int expectedVersion, CancellationToken ct = default) =>
        await db.ExecuteAsync("""
          update habitflow.user_onboarding_progress set current_step=@CurrentStep,selected_objective_slug=@SelectedObjectiveSlug,
          available_minutes=@AvailableMinutes,preferred_frequency=@PreferredFrequency,preferred_days=@PreferredDays,
          preferred_time=@PreferredTime,selected_template_ids=@SelectedTemplateIds,selected_collection_id=@SelectedCollectionId,
          create_goal=@CreateGoal,goal_target_type=@GoalTargetType,goal_target_value=@GoalTargetValue,last_activity_at=now(),
          completed_at=@CompletedAt,skipped_at=@SkippedAt,version=version+1
          where client_id=@ClientId and user_id=@UserId and version=@expectedVersion
          """, new { p.ClientId,p.UserId,CurrentStep=(short)p.CurrentStep,p.SelectedObjectiveSlug,p.AvailableMinutes,p.PreferredFrequency,p.PreferredDays,p.PreferredTime,p.SelectedTemplateIds,p.SelectedCollectionId,p.CreateGoal,p.GoalTargetType,p.GoalTargetValue,p.CompletedAt,p.SkippedAt,expectedVersion }, ct) == 1;
}

public sealed class UserOnboardingDraftRepository(SqlExecutor db) : IUserOnboardingDraftRepository
{
    public async Task<IReadOnlyList<UserOnboardingDraftItem>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserOnboardingDraftItem>("select id,client_id,user_id,template_id,collection_id,name,frequency,days,target_per_week,preferred_time,color,category,is_required,sort_order from habitflow.user_onboarding_draft_items where client_id=@clientId and user_id=@userId order by sort_order", new { clientId,userId }, ct)).ToList();
    public async Task ReplaceAsync(Guid clientId, Guid userId, IReadOnlyCollection<UserOnboardingDraftItem> items, CancellationToken ct = default)
    {
        await DeleteAsync(clientId,userId,ct);
        foreach (var x in items) await db.ExecuteAsync("insert into habitflow.user_onboarding_draft_items(id,client_id,user_id,template_id,collection_id,name,frequency,days,target_per_week,preferred_time,color,category,is_required,sort_order) values(@Id,@ClientId,@UserId,@TemplateId,@CollectionId,@Name,@Frequency,@Days,@TargetPerWeek,@PreferredTime,@Color,@Category,@IsRequired,@SortOrder)", x, ct);
    }
    public async Task DeleteAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        _ = await db.ExecuteAsync("delete from habitflow.user_onboarding_draft_items where client_id=@clientId and user_id=@userId", new { clientId,userId }, ct);
}
