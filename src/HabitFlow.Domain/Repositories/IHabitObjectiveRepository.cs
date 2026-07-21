namespace HabitFlow.Domain;

public interface IHabitObjectiveRepository
{
    Task<IReadOnlyList<HabitObjective>> ListActiveAsync(CancellationToken ct = default);
    Task<HabitObjective?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<HabitObjective>> ListAllForAdminAsync(CancellationToken ct = default);
    Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}
