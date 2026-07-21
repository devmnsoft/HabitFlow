using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitObjectiveRepository(SqlExecutor db) : IHabitObjectiveRepository
{
    private const string Columns = "id, slug, name, description, icon, sort_order, is_active, created_at";

    public async Task<IReadOnlyList<HabitObjective>> ListActiveAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitObjective>($"select {Columns} from habitflow.habit_objectives where is_active = true order by sort_order, name", ct: ct)).ToList();

    public Task<HabitObjective?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitObjective>($"select {Columns} from habitflow.habit_objectives where slug = @slug and is_active = true", new { slug }, ct);

    public async Task<IReadOnlyList<HabitObjective>> ListAllForAdminAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitObjective>($"select {Columns} from habitflow.habit_objectives order by sort_order, name", ct: ct)).ToList();

    public Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.habit_objectives set is_active = @isActive where id = @id", new { id, isActive }, ct);
}
