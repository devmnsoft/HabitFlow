using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class ProductTipService(IProductTipRepository repository, TimeProvider clock)
{
    public Task<ProductTip?> GetNextAsync(Guid clientId, Guid userId, string path, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) return Task.FromResult<ProductTip?>(null);
        var normalized = string.IsNullOrWhiteSpace(path) ? "/" : path.ToLowerInvariant();
        return repository.GetNextAsync(clientId, userId, normalized, ct);
    }

    public Task<bool> DismissAsync(Guid clientId, Guid userId, Guid tipId, CancellationToken ct = default) =>
        repository.DismissAsync(clientId, userId, tipId, clock.GetUtcNow().UtcDateTime, ct);

    public Task<int> ReopenAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        repository.ReopenAsync(clientId, userId, ct);
}

