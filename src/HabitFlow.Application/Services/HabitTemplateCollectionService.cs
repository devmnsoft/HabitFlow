using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitTemplateCollectionDetails(HabitTemplateCollection Collection, IReadOnlyList<HabitTemplateCollectionItem> Items);

public sealed class HabitTemplateCollectionService(IHabitTemplateCollectionRepository collections, PlanEntitlementService plans)
{
    public Task<IReadOnlyList<HabitTemplateCollection>> ListAsync(CancellationToken ct = default) => collections.ListPublishedAsync(ct);
    public async Task<Result<HabitTemplateCollectionDetails>> GetAsync(string slug, Guid userId, CancellationToken ct = default)
    {
        var collection = await collections.GetPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct);
        if (collection is null) return Result<HabitTemplateCollectionDetails>.Failure("collection.not_found", "Coleção não encontrada.");
        if (!string.Equals(collection.MinimumPlanCode, PlanCodes.Free, StringComparison.OrdinalIgnoreCase) &&
            !await plans.CanUseFullLibraryAsync(userId, ct))
            return Result<HabitTemplateCollectionDetails>.Failure("collection.plan_required", "Esta coleção requer acesso à Biblioteca completa.");
        return Result<HabitTemplateCollectionDetails>.Success(new(collection, await collections.ListItemsAsync(collection.Id, ct)));
    }
}
