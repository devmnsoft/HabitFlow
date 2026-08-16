using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateFavoriteRepository(SqlExecutor db) : IHabitTemplateFavoriteRepository
{
    public async Task<bool> ExistsAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        await db.QuerySingleOrDefaultAsync<bool>("select exists(select 1 from habitflow.habit_template_favorites where client_id=@clientId and user_id=@userId and template_id=@templateId)", new { clientId, userId, templateId }, ct);

    public Task AddAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.habit_template_favorites(client_id,user_id,template_id) select @clientId,@userId,@templateId where exists(select 1 from habitflow.users where id=@userId and client_id=@clientId) and exists(select 1 from habitflow.habit_templates where id=@templateId and is_active=true and published_at is not null) on conflict do nothing", new { clientId, userId, templateId }, ct);

    public Task RemoveAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) =>
        db.ExecuteAsync("delete from habitflow.habit_template_favorites where client_id=@clientId and user_id=@userId and template_id=@templateId", new { clientId, userId, templateId }, ct);

    public async Task<IReadOnlyList<HabitTemplate>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.Select + """
            join habitflow.habit_template_favorites f
              on f.template_id = t.id
             and f.client_id = @clientId
             and f.user_id = @userId
            where t.is_active = true
              and t.published_at is not null
            order by t.sort_order, t.name
            """, new { clientId, userId }, ct);

        return rows.Select(HabitTemplateProjection.Map).ToList();
    }
}
