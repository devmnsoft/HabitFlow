using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateRepository(SqlExecutor db) : IHabitTemplateRepository
{
    public async Task<IReadOnlyList<HabitTemplate>> ListActiveAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplate>($"select {Columns} from habitflow.habit_templates where is_active=true and published_at is not null order by is_featured desc,sort_order,name", null, ct)).ToList();
    private const string Columns = "id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty, estimated_time_minutes, benefit_text, sort_order, is_active, created_at, updated_at, coalesce((select sum(1 << d)::int from unnest(suggested_days) d),127) as suggested_days, suggested_target_per_week, suggested_reminder_time, icon_code, why_it_helps, how_to_start, first_action, tags, minimum_plan_code, is_featured, content_version, published_at";

    public async Task<IReadOnlyList<HabitTemplate>> ListActiveByObjectiveAsync(Guid objectiveId, CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplate>($"select {Columns} from habitflow.habit_templates where objective_id = @objectiveId and is_active = true order by sort_order, name", new { objectiveId }, ct)).ToList();

    public Task<HabitTemplate?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitTemplate>($"select {Columns} from habitflow.habit_templates where id = @id", new { id }, ct);

    public async Task<IReadOnlyList<HabitTemplate>> ListAllForAdminAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplate>($"select {Columns} from habitflow.habit_templates order by category, sort_order, name", ct: ct)).ToList();

    public Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.habit_templates set is_active = @isActive, updated_at = now() where id = @id", new { id, isActive }, ct);
}
