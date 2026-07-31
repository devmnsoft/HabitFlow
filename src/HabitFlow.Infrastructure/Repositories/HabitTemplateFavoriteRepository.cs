using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateFavoriteRepository(SqlExecutor db) : IHabitTemplateFavoriteRepository
{
    private const string Columns = "t.id, t.objective_id, t.name, t.description, t.category, t.suggested_frequency, t.suggested_color, t.difficulty, t.estimated_time_minutes, t.benefit_text, t.sort_order, t.is_active, t.created_at, t.updated_at, coalesce((select sum(1 << d)::int from unnest(t.suggested_days) d),127) as suggested_days, t.suggested_target_per_week, t.suggested_reminder_time, t.icon_code, t.why_it_helps, t.how_to_start, t.first_action, t.tags, t.minimum_plan_code, t.is_featured, t.content_version, t.published_at";

    public async Task<bool> ExistsAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        await db.QuerySingleOrDefaultAsync<bool>("select exists(select 1 from habitflow.habit_template_favorites where client_id=@clientId and user_id=@userId and template_id=@templateId)", new { clientId, userId, templateId }, ct);

    public Task AddAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.habit_template_favorites(client_id,user_id,template_id) select @clientId,@userId,@templateId where exists(select 1 from habitflow.users where id=@userId and client_id=@clientId) and exists(select 1 from habitflow.habit_templates where id=@templateId and is_active=true and published_at is not null) on conflict do nothing", new { clientId, userId, templateId }, ct);

    public Task RemoveAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        db.ExecuteAsync("delete from habitflow.habit_template_favorites where client_id=@clientId and user_id=@userId and template_id=@templateId", new { clientId, userId, templateId }, ct);

    public async Task<IReadOnlyList<HabitTemplate>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplate>($"select {Columns} from habitflow.habit_templates t join habitflow.habit_template_favorites f on f.template_id=t.id and f.client_id=@clientId and f.user_id=@userId where t.is_active=true and t.published_at is not null order by t.sort_order,t.name", new { clientId, userId }, ct)).ToList();
}
