using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class HabitLibraryFallbackProvider
{
    private static readonly DateTime SeededAt = new(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);

    public IReadOnlyList<HabitObjective> GetObjectives() => HabitLibraryFallback.Objectives;
    public HabitObjective? GetObjectiveBySlug(string slug) => GetObjectives().FirstOrDefault(o => string.Equals(o.Slug, slug, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<HabitTemplate> GetTemplatesBySlug(string slug)
    {
        var objective = GetObjectiveBySlug(slug);
        return objective is null ? Array.Empty<HabitTemplate>() : HabitLibraryFallback.TemplatesFor(objective.Id, slug);
    }
    public HabitTemplate? GetTemplate(Guid id) => GetObjectives().SelectMany(o => HabitLibraryFallback.TemplatesFor(o.Id, o.Slug)).FirstOrDefault(t => t.Id == id);

    public static HabitTemplate Template(string id, Guid objectiveId, string name, string description, string category, string color, HabitDifficulty difficulty, int minutes, string benefit, int sort) =>
        new(Guid.Parse(id), objectiveId, name, description, category, "Daily", color, difficulty, minutes, benefit, sort, true, SeededAt, SeededAt);
}
