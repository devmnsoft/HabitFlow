namespace HabitFlow.Application;

public interface IProgressCalendarRepository
{
    Task<ProgressData> GetProgressDataAsync(
        Guid clientId,
        Guid userId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct = default);
}
