using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

internal static class HabitTemplateProjection
{
    internal const string SelectFromTemplates = """
        select
            t.id as "Id",
            t.objective_id as "ObjectiveId",
            t.name as "Name",
            t.description as "Description",
            t.category as "Category",
            t.suggested_frequency as "SuggestedFrequency",
            t.suggested_color as "SuggestedColor",
            t.difficulty as "Difficulty",
            t.estimated_time_minutes as "EstimatedTimeMinutes",
            t.benefit_text as "BenefitText",
            t.sort_order as "SortOrder",
            t.is_active as "IsActive",
            t.created_at as "CreatedAt",
            t.updated_at as "UpdatedAt",
            coalesce((select sum(1 << d)::int from unnest(t.suggested_days) d), 127)::int as "SuggestedDays",
            t.suggested_target_per_week as "SuggestedTargetPerWeek",
            t.suggested_reminder_time as "SuggestedReminderTime",
            t.icon_code as "IconCode",
            t.why_it_helps as "WhyItHelps",
            t.how_to_start as "HowToStart",
            t.first_action as "FirstAction",
            coalesce(t.tags, array[]::text[]) as "Tags",
            coalesce(t.minimum_plan_code, 'free') as "MinimumPlanCode",
            coalesce(t.is_featured, false) as "IsFeatured",
            coalesce(t.content_version, 1) as "ContentVersion",
            t.published_at as "PublishedAt"
        from habitflow.habit_templates t
        """;

    internal static string WithClause(string clause)
    {
        if (string.IsNullOrWhiteSpace(clause))
            return SelectFromTemplates;

        return $"{SelectFromTemplates.TrimEnd()}{Environment.NewLine}{clause.TrimStart()}";
    }

    internal static HabitTemplate Map(HabitTemplateRow row) => new(
        row.Id,
        row.ObjectiveId,
        row.Name,
        row.Description,
        row.Category,
        row.SuggestedFrequency,
        row.SuggestedColor,
        ParseDifficulty(row.Difficulty),
        row.EstimatedTimeMinutes,
        row.BenefitText,
        row.SortOrder,
        row.IsActive,
        row.CreatedAt,
        row.UpdatedAt,
        NormalizeSuggestedDays(row.SuggestedDays),
        row.SuggestedTargetPerWeek,
        row.SuggestedReminderTime,
        row.IconCode,
        row.WhyItHelps,
        row.HowToStart,
        row.FirstAction,
        row.Tags ?? [],
        string.IsNullOrWhiteSpace(row.MinimumPlanCode) ? "free" : row.MinimumPlanCode,
        row.IsFeatured,
        row.ContentVersion <= 0 ? 1 : row.ContentVersion,
        row.PublishedAt);

    private static HabitDifficulty ParseDifficulty(string? value) =>
        Enum.TryParse<HabitDifficulty>(value, ignoreCase: true, out var parsed)
            ? parsed
            : HabitDifficulty.Easy;

    private static SuggestedWeekDays NormalizeSuggestedDays(int value) =>
        value <= 0 ? SuggestedWeekDays.EveryDay : (SuggestedWeekDays)value;
}

internal sealed class HabitTemplateRow
{
    public Guid Id { get; init; }
    public Guid ObjectiveId { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string SuggestedFrequency { get; init; } = "";
    public string SuggestedColor { get; init; } = "";
    public string Difficulty { get; init; } = "";
    public int? EstimatedTimeMinutes { get; init; }
    public string? BenefitText { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int SuggestedDays { get; init; } = 127;
    public int? SuggestedTargetPerWeek { get; init; }
    public TimeOnly? SuggestedReminderTime { get; init; }
    public string? IconCode { get; init; }
    public string? WhyItHelps { get; init; }
    public string? HowToStart { get; init; }
    public string? FirstAction { get; init; }
    public string[]? Tags { get; init; }
    public string MinimumPlanCode { get; init; } = "free";
    public bool IsFeatured { get; init; }
    public int ContentVersion { get; init; } = 1;
    public DateTime? PublishedAt { get; init; }
}
