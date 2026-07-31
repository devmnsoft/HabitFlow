namespace HabitFlow.Domain;

public interface IHabitTemplateFavoriteRepository
{
    Task<bool> ExistsAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default);
    Task AddAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default);
    Task RemoveAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitTemplate>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default);
}
