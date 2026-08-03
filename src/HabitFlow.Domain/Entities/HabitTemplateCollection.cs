namespace HabitFlow.Domain;

public sealed record HabitTemplateCollection(
    Guid Id, string Slug, string Name, string Description, Guid? ObjectiveId,
    string? IconCode, int? EstimatedTimeMinutes, string Difficulty,
    string MinimumPlanCode, bool IsFeatured, string Status, int ContentVersion,
    int SortOrder, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record HabitTemplateCollectionItem(
    Guid CollectionId, Guid TemplateId, int SortOrder, bool IsRequired,
    TimeOnly? DefaultReminderTime, bool CanCustomize, HabitTemplate Template);

public interface IHabitTemplateCollectionRepository
{
    Task<IReadOnlyList<HabitTemplateCollection>> ListPublishedAsync(CancellationToken ct = default);
    Task<HabitTemplateCollection?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);
    Task<HabitTemplateCollection?> GetPublishedAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<HabitTemplateCollectionItem>> ListItemsAsync(Guid collectionId, CancellationToken ct = default);
}
