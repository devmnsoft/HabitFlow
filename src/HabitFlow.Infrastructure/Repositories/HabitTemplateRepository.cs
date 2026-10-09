using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateRepository(SqlExecutor db) : IHabitTemplateRepository
{
    public async Task<IReadOnlyList<HabitTemplate>> ListActiveAsync(Guid? clientId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.WithClause("""
            where t.is_active = true
              and t.published_at is not null
              and (t.client_id is null or t.client_id = @clientId)
            order by t.is_featured desc, t.sort_order, t.name
            """), new { clientId }, ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public async Task<IReadOnlyList<HabitTemplate>> ListActiveByObjectiveAsync(Guid objectiveId, Guid? clientId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.WithClause("""
            where t.objective_id = @objectiveId
              and t.is_active = true
              and (t.client_id is null or t.client_id = @clientId)
            order by t.sort_order, t.name
            """), new { objectiveId, clientId }, ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public Task CreateAsync(HabitTemplateDraft draft, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.habit_templates(
                id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty,
                estimated_time_minutes, benefit_text, sort_order, is_active, minimum_plan_code, audience, goal_text,
                created_by, client_id, published_at, language, origin, marketplace_status)
            values (
                @Id, @ObjectiveId, @Name, @Description, @Category, @Frequency, '#16A34A', @Difficulty,
                @Minutes, @Goal, 100, true, @MinimumPlan, @Audience, @Goal, @CreatedBy, @ClientId, now(),
                'pt-BR', case when @ClientId is null then 'SuperAdmin' else 'Tenant' end, 'Published')
            """, draft, ct);

    public async Task<HabitTemplate?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<HabitTemplateRow>(HabitTemplateProjection.WithClause("""
            where t.id = @id
            """), new { id }, ct);
        return row is null ? null : HabitTemplateProjection.Map(row);
    }

    public async Task<IReadOnlyList<HabitTemplate>> ListAllForAdminAsync(CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.WithClause("""
            order by t.category, t.sort_order, t.name
            """), ct: ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.habit_templates set is_active = @isActive, updated_at = now() where id = @id", new { id, isActive }, ct);
}
