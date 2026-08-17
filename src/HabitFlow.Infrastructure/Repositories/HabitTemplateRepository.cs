using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateRepository(SqlExecutor db) : IHabitTemplateRepository
{
    public async Task<IReadOnlyList<HabitTemplate>> ListActiveAsync(CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.SelectFromTemplates +
            " where t.is_active = true and t.published_at is not null order by t.is_featured desc, t.sort_order, t.name", null, ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public async Task<IReadOnlyList<HabitTemplate>> ListActiveByObjectiveAsync(Guid objectiveId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.SelectFromTemplates +
            " where t.objective_id = @objectiveId and t.is_active = true order by t.sort_order, t.name", new { objectiveId }, ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public async Task<HabitTemplate?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<HabitTemplateRow>(HabitTemplateProjection.SelectFromTemplates + " where t.id = @id", new { id }, ct);
        return row is null ? null : HabitTemplateProjection.Map(row);
    }

    public async Task<IReadOnlyList<HabitTemplate>> ListAllForAdminAsync(CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitTemplateRow>(HabitTemplateProjection.SelectFromTemplates + " order by t.category, t.sort_order, t.name", ct: ct);
        return rows.Select(HabitTemplateProjection.Map).ToList();
    }

    public Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.habit_templates set is_active = @isActive, updated_at = now() where id = @id", new { id, isActive }, ct);
}
