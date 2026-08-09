using System.Data;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitScheduleExceptionRepository(SqlExecutor db) : IHabitScheduleExceptionRepository
{
    private const string SelectColumns = """
        select id as Id, client_id as ClientId, user_id as UserId, habit_id as HabitId,
               local_date as LocalDate, type as Type, destination_date as DestinationDate,
               reason as Reason, version as Version, created_at as CreatedAt, updated_at as UpdatedAt
        from habitflow.habit_schedule_exceptions
        """;

    public async Task<IReadOnlyList<HabitScheduleException>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<HabitScheduleExceptionRow>(SelectColumns +
            " where client_id=@clientId and user_id=@userId and (local_date between @from and @to or destination_date between @from and @to)",
            new { clientId, userId, from, to }, ct);
        return rows.Select(MapHabitScheduleException).ToList();
    }

    public async Task<HabitScheduleException?> GetAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<HabitScheduleExceptionRow>(SelectColumns +
            " where client_id=@clientId and user_id=@userId and habit_id=@habitId and local_date=@localDate",
            new { clientId, userId, habitId, localDate }, ct);
        return row is null ? null : MapHabitScheduleException(row);
    }

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

    private static HabitScheduleException MapHabitScheduleException(HabitScheduleExceptionRow row)
    {
        if (!Enum.TryParse<HabitScheduleExceptionType>(row.Type, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            throw new DataException("Tipo de exceção de agenda inválido no banco.");
        if (type == HabitScheduleExceptionType.Moved && row.DestinationDate is null)
            throw new DataException("A data de destino é obrigatória para uma exceção de agenda movida.");
        if (type != HabitScheduleExceptionType.Moved && row.DestinationDate is not null)
            throw new DataException("A data de destino só é permitida para uma exceção de agenda movida.");

        return new(row.Id, row.ClientId, row.UserId, row.HabitId, row.LocalDate, type,
            row.DestinationDate, row.Reason, row.Version, ToUtcOffset(row.CreatedAt), ToUtcOffset(row.UpdatedAt));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }

    private sealed class HabitScheduleExceptionRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public DateOnly LocalDate { get; init; }
        public string Type { get; init; } = string.Empty;
        public DateOnly? DestinationDate { get; init; }
        public string? Reason { get; init; }
        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}

public sealed class DailyRoutineOverrideRepository(SqlExecutor db) : IDailyRoutineOverrideRepository
{
    public async Task<IReadOnlyList<DailyRoutineOverride>> ListAsync(Guid clientId, Guid userId, DateOnly localDate, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<DailyRoutineOverrideRow>("""
            select id as Id, client_id as ClientId, user_id as UserId, habit_id as HabitId,
                   local_date as LocalDate, preferred_time as PreferredTime, sort_order as SortOrder,
                   version as Version, created_at as CreatedAt, updated_at as UpdatedAt
            from habitflow.daily_routine_overrides
            where client_id=@clientId and user_id=@userId and local_date=@localDate
            """, new { clientId,userId,localDate }, ct);
        return rows.Select(MapDailyRoutineOverride).ToList();
    }

    public Task UpsertAsync(DailyRoutineOverride value, int expectedVersion, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.daily_routine_overrides(id,client_id,user_id,habit_id,local_date,preferred_time,sort_order,version,created_at,updated_at)
        select @Id,@ClientId,@UserId,h.id,@LocalDate,@PreferredTime,@SortOrder,1,@CreatedAt,@UpdatedAt from habitflow.habits h where h.id=@HabitId and h.client_id=@ClientId and h.user_id=@UserId
        on conflict(client_id,user_id,habit_id,local_date) do update set preferred_time=excluded.preferred_time,sort_order=excluded.sort_order,version=habitflow.daily_routine_overrides.version+1,updated_at=excluded.updated_at where habitflow.daily_routine_overrides.version=@expectedVersion
        """, new { value.Id,value.ClientId,value.UserId,value.HabitId,value.LocalDate,value.PreferredTime,value.SortOrder,value.CreatedAt,value.UpdatedAt,expectedVersion }, ct);

    public async Task<bool> DeleteAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, int expectedVersion, CancellationToken ct = default) =>
        await db.ExecuteAsync("delete from habitflow.daily_routine_overrides where client_id=@clientId and user_id=@userId and habit_id=@habitId and local_date=@localDate and version=@expectedVersion",
            new { clientId, userId, habitId, localDate, expectedVersion }, ct) == 1;

    private static DailyRoutineOverride MapDailyRoutineOverride(DailyRoutineOverrideRow row) =>
        new(row.Id, row.ClientId, row.UserId, row.HabitId, row.LocalDate, row.PreferredTime,
            row.SortOrder, row.Version, ToUtcOffset(row.CreatedAt), ToUtcOffset(row.UpdatedAt));

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }

    private sealed class DailyRoutineOverrideRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public Guid HabitId { get; init; }
        public DateOnly LocalDate { get; init; }
        public TimeOnly? PreferredTime { get; init; }
        public int SortOrder { get; init; }
        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}

public sealed class WeeklyReviewRepository(SqlExecutor db) : IWeeklyReviewRepository
{
    public async Task<WeeklyReview?> GetAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<WeeklyReviewRow>("""
            select id as Id, client_id as ClientId, user_id as UserId, period_start as PeriodStart,
                   period_end as PeriodEnd, status as Status, idempotency_key as IdempotencyKey,
                   version as Version, created_at as CreatedAt, completed_at as CompletedAt
            from habitflow.weekly_reviews
            where client_id=@clientId and user_id=@userId and period_start=@periodStart
            """, new {clientId,userId,periodStart}, ct);
        return row is null ? null : MapWeeklyReview(row);
    }

    public async Task<WeeklyReview> CompleteAsync(WeeklyReview value, CancellationToken ct = default)
    {
        await db.ExecuteAsync("insert into habitflow.weekly_reviews(id,client_id,user_id,period_start,period_end,status,idempotency_key,version,created_at,completed_at) values(@Id,@ClientId,@UserId,@PeriodStart,@PeriodEnd,'Completed',@IdempotencyKey,1,@CreatedAt,@CompletedAt) on conflict(client_id,user_id,period_start) do nothing", value, ct);
        return (await GetAsync(value.ClientId,value.UserId,value.PeriodStart,ct))!;
    }

    private static WeeklyReview MapWeeklyReview(WeeklyReviewRow row) =>
        new(row.Id, row.ClientId, row.UserId, row.PeriodStart, row.PeriodEnd, row.Status,
            row.IdempotencyKey, row.Version, ToUtcOffset(row.CreatedAt), ToUtcOffset(row.CompletedAt));

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }

    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value.HasValue ? ToUtcOffset(value.Value) : null;

    private sealed class WeeklyReviewRow
    {
        public Guid Id { get; init; }
        public Guid ClientId { get; init; }
        public Guid UserId { get; init; }
        public DateOnly PeriodStart { get; init; }
        public DateOnly PeriodEnd { get; init; }
        public string Status { get; init; } = string.Empty;
        public string IdempotencyKey { get; init; } = string.Empty;
        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
}
