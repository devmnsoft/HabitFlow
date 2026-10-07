namespace HabitFlow.Domain;

public interface IHabitTemplateRepository
{
    Task<IReadOnlyList<HabitTemplate>> ListActiveAsync(Guid? clientId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitTemplate>> ListActiveByObjectiveAsync(Guid objectiveId, Guid? clientId, CancellationToken ct = default);
    Task CreateAsync(HabitTemplateDraft draft, CancellationToken ct = default);
    Task<HabitTemplate?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<HabitTemplate>> ListAllForAdminAsync(CancellationToken ct = default);
    Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}
