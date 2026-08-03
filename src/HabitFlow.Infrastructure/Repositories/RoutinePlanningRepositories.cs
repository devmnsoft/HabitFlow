using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitScheduleExceptionRepository(SqlExecutor db) : IHabitScheduleExceptionRepository
{
    public async Task<IReadOnlyList<HabitScheduleException>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        (await db.QueryAsync<HabitScheduleException>("select id,client_id,user_id,habit_id,local_date,type,destination_date,reason,version,created_at,updated_at from habitflow.habit_schedule_exceptions where client_id=@clientId and user_id=@userId and (local_date between @from and @to or destination_date between @from and @to)", new { clientId, userId, from, to }, ct)).ToList();

    public Task<HabitScheduleException?> GetAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitScheduleException>("select id,client_id,user_id,habit_id,local_date,type,destination_date,reason,version,created_at,updated_at from habitflow.habit_schedule_exceptions where client_id=@clientId and user_id=@userId and habit_id=@habitId and local_date=@localDate", new { clientId,userId,habitId,localDate }, ct);

    public async Task<ScheduleExceptionMutationResult> UpsertAsync(HabitScheduleException value, int expectedVersion, CancellationToken ct = default)
    {
        var affected = await db.ExecuteAsync("""
        insert into habitflow.habit_schedule_exceptions(id,client_id,user_id,habit_id,local_date,type,destination_date,reason,version,created_at,updated_at)
        select @Id,@ClientId,@UserId,h.id,@LocalDate,@Type,@DestinationDate,@Reason,1,@CreatedAt,@UpdatedAt from habitflow.habits h
        where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId and @expectedVersion=0
        on conflict(client_id,user_id,habit_id,local_date) do update
        set type=excluded.type,destination_date=excluded.destination_date,reason=excluded.reason,version=habitflow.habit_schedule_exceptions.version+1,updated_at=excluded.updated_at
        where habitflow.habit_schedule_exceptions.client_id=@ClientId
          and habitflow.habit_schedule_exceptions.user_id=@UserId
          and habitflow.habit_schedule_exceptions.habit_id=@HabitId
          and habitflow.habit_schedule_exceptions.local_date=@LocalDate
          and habitflow.habit_schedule_exceptions.version=@expectedVersion
        """, new { value.Id, value.ClientId, value.UserId, value.HabitId, value.LocalDate, Type=value.Type.ToString(), value.DestinationDate, value.Reason, value.CreatedAt, value.UpdatedAt, expectedVersion }, ct);
        var current = await GetAsync(value.ClientId,value.UserId,value.HabitId,value.LocalDate,ct);
        if (affected == 0) return new(ScheduleExceptionMutationStatus.Conflict,current?.Version ?? 0);
        return new(expectedVersion == 0 ? ScheduleExceptionMutationStatus.Created : ScheduleExceptionMutationStatus.Updated,current!.Version);
    }

    public async Task<ScheduleExceptionMutationResult> DeleteAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, int expectedVersion, CancellationToken ct = default)
    {
        var affected = await db.ExecuteAsync("delete from habitflow.habit_schedule_exceptions where client_id=@clientId and user_id=@userId and habit_id=@habitId and local_date=@localDate and version=@expectedVersion", new { clientId,userId,habitId,localDate,expectedVersion }, ct);
        if (affected == 1) return new(ScheduleExceptionMutationStatus.Updated,0);
        var current = await GetAsync(clientId,userId,habitId,localDate,ct);
        return new(ScheduleExceptionMutationStatus.Conflict,current?.Version ?? 0);
    }
}

public sealed class DailyRoutineOverrideRepository(SqlExecutor db) : IDailyRoutineOverrideRepository
{
    public async Task<IReadOnlyList<DailyRoutineOverride>> ListAsync(Guid clientId, Guid userId, DateOnly localDate, CancellationToken ct = default) => (await db.QueryAsync<DailyRoutineOverride>("select id,client_id,user_id,habit_id,local_date,preferred_time,sort_order,version,created_at,updated_at from habitflow.daily_routine_overrides where client_id=@clientId and user_id=@userId and local_date=@localDate", new { clientId,userId,localDate }, ct)).ToList();
    public Task UpsertAsync(DailyRoutineOverride value, int expectedVersion, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.daily_routine_overrides(id,client_id,user_id,habit_id,local_date,preferred_time,sort_order,version,created_at,updated_at)
        select @Id,@ClientId,@UserId,h.id,@LocalDate,@PreferredTime,@SortOrder,1,@CreatedAt,@UpdatedAt from habitflow.habits h where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId
        on conflict(client_id,user_id,habit_id,local_date) do update set preferred_time=excluded.preferred_time,sort_order=excluded.sort_order,version=habitflow.daily_routine_overrides.version+1,updated_at=excluded.updated_at where habitflow.daily_routine_overrides.version=@expectedVersion
        """, new { value.Id,value.ClientId,value.UserId,value.HabitId,value.LocalDate,value.PreferredTime,value.SortOrder,value.CreatedAt,value.UpdatedAt,expectedVersion }, ct);
}

public sealed class WeeklyReviewRepository(SqlExecutor db) : IWeeklyReviewRepository
{
    public Task<WeeklyReview?> GetAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<WeeklyReview>("select id,client_id,user_id,period_start,period_end,status,idempotency_key,version,created_at,completed_at from habitflow.weekly_reviews where client_id=@clientId and user_id=@userId and period_start=@periodStart", new {clientId,userId,periodStart}, ct);
    public async Task<WeeklyReview> CompleteAsync(WeeklyReview value, CancellationToken ct = default)
    {
        await db.ExecuteAsync("insert into habitflow.weekly_reviews(id,client_id,user_id,period_start,period_end,status,idempotency_key,version,created_at,completed_at) values(@Id,@ClientId,@UserId,@PeriodStart,@PeriodEnd,'Completed',@IdempotencyKey,1,@CreatedAt,@CompletedAt) on conflict(client_id,user_id,period_start) do nothing", value, ct);
        return (await GetAsync(value.ClientId,value.UserId,value.PeriodStart,ct))!;
    }
}
