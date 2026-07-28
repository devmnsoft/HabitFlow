using HabitFlow.Application;

namespace HabitFlow.Infrastructure;

public sealed class ProgressCalendarRepository(SqlExecutor db) : IProgressCalendarRepository
{
    public async Task<ProgressData> GetProgressDataAsync(Guid clientId, Guid userId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        const string habitsSql = """
            select h.id as Id, h.name as Name, h.category as Category, h.is_archived as IsArchived,
                   h.archived_at as ArchivedAt, h.created_at as CreatedAt, h.frequency_type as FrequencyType,
                   h.reminder_time as ReminderTime
              from habitflow.habits h
              join habitflow.users u on u.id = h.user_id
             where h.user_id = @userId and u.client_id = @clientId
               and h.created_at < (@end::date + interval '1 day')
               and (h.archived_at is null or h.archived_at >= @start::date)
             order by h.sort_order, h.created_at
            """;
        var habits = (await db.QueryAsync<ProgressHabitRow>(habitsSql, new { clientId, userId, start, end }, ct)).ToList();
        var ids = habits.Select(x => x.Id).ToArray();
        IReadOnlyList<ProgressWeekDayRow> weekDays = ids.Length == 0
            ? Array.Empty<ProgressWeekDayRow>()
            : (await db.QueryAsync<ProgressWeekDayRow>("select habit_id as HabitId, day_of_week as DayOfWeek from habitflow.habit_week_days where habit_id = any(@ids)", new { ids }, ct)).ToList();
        const string completionsSql = """
            select c.habit_id as HabitId, c.completed_date as CompletedDate
              from habitflow.habit_completions c
              join habitflow.habits h on h.id = c.habit_id
              join habitflow.users u on u.id = c.user_id
             where c.user_id = @userId and h.user_id = @userId and u.client_id = @clientId
               and c.completed_date between @start and @end
             group by c.habit_id, c.completed_date
            """;
        var completions = (await db.QueryAsync<ProgressCompletionRow>(completionsSql, new { clientId, userId, start, end }, ct)).ToList();
        var plan = await db.QuerySingleOrDefaultAsync<string>("select plan::text from habitflow.users where id = @userId and client_id = @clientId", new { clientId, userId }, ct) ?? "Free";
        return new(habits, weekDays, completions, plan);
    }
}
