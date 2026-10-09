namespace HabitFlow.Domain;

public interface IHabitAutomationRepository
{
    Task<IReadOnlyList<HabitAutomation>> ListForUserAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task CreateAsync(HabitAutomation automation, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, Guid clientId, Guid userId, HabitAutomationStatus status, CancellationToken ct = default);
}
