namespace HabitFlow.Domain;

public interface IHabitScheduleExceptionRepository
{
    Task<IReadOnlyList<HabitScheduleException>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task UpsertAsync(HabitScheduleException exception, CancellationToken ct = default);
    Task DeleteAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, int expectedVersion, CancellationToken ct = default);
}

public interface IDailyRoutineOverrideRepository
{
    Task<IReadOnlyList<DailyRoutineOverride>> ListAsync(Guid clientId, Guid userId, DateOnly localDate, CancellationToken ct = default);
    Task UpsertAsync(DailyRoutineOverride value, int expectedVersion, CancellationToken ct = default);
}

public interface IWeeklyReviewRepository
{
    Task<WeeklyReview?> GetAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default);
    Task<WeeklyReview> CompleteAsync(WeeklyReview review, CancellationToken ct = default);
}
