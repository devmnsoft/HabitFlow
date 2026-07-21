using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitWeekDayRepository(SqlExecutor db) : IHabitWeekDayRepository
{
    public async Task<IReadOnlyList<HabitWeekDay>> ListByHabitAsync(Guid habitId, CancellationToken ct = default) =>
        (await db.QueryAsync<HabitWeekDay>("select id, habit_id, day_of_week, created_at from habitflow.habit_week_days where habit_id=@habitId order by day_of_week", new { habitId }, ct)).ToList();

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<HabitWeekDay>>> ListByHabitsAsync(IEnumerable<Guid> habitIds, CancellationToken ct = default)
    {
        var ids = habitIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, IReadOnlyList<HabitWeekDay>>();
        var rows = (await db.QueryAsync<HabitWeekDay>("select id, habit_id, day_of_week, created_at from habitflow.habit_week_days where habit_id = any(@ids) order by habit_id, day_of_week", new { ids }, ct)).ToList();
        return rows.GroupBy(x => x.HabitId).ToDictionary(g => g.Key, g => (IReadOnlyList<HabitWeekDay>)g.ToList());
    }

    public async Task ReplaceAsync(Guid habitId, IReadOnlyCollection<int> days, CancellationToken ct = default)
    {
        await db.ExecuteAsync("delete from habitflow.habit_week_days where habit_id=@habitId", new { habitId }, ct);
        foreach (var day in days.Distinct().OrderBy(x => x))
            await db.ExecuteAsync("insert into habitflow.habit_week_days(id,habit_id,day_of_week,created_at) values(@id,@habitId,@day,now())", new { id = Guid.NewGuid(), habitId, day }, ct);
    }
}
