namespace HabitFlow.Domain;

public sealed record HabitObjective(Guid Id, string Slug, string Name, string Description, string? Icon, int SortOrder, bool IsActive, DateTime CreatedAt);
