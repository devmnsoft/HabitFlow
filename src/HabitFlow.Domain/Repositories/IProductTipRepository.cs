namespace HabitFlow.Domain;

public interface IProductTipRepository
{
    Task<ProductTip?> GetNextAsync(Guid clientId, Guid userId, string path, CancellationToken ct = default);
    Task<bool> DismissAsync(Guid clientId, Guid userId, Guid tipId, DateTime occurredAt, CancellationToken ct = default);
    Task<int> ReopenAsync(Guid clientId, Guid userId, CancellationToken ct = default);
}

