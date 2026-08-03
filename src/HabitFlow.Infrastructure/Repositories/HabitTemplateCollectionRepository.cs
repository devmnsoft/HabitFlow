using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateCollectionRepository(SqlExecutor db) : IHabitTemplateCollectionRepository
{
    private const string Columns = "id,slug,name,description,objective_id,icon_code,estimated_time_minutes,difficulty,minimum_plan_code,is_featured,status,content_version,sort_order,created_at,updated_at";

    public async Task<IReadOnlyList<HabitTemplateCollection>> ListPublishedAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplateCollection>($"select {Columns} from habitflow.habit_template_collections where status='Published' order by sort_order,name", ct: ct)).ToList();

    public Task<HabitTemplateCollection?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitTemplateCollection>($"select {Columns} from habitflow.habit_template_collections where slug=@slug and status='Published'", new { slug }, ct);

    public Task<HabitTemplateCollection?> GetPublishedAsync(Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitTemplateCollection>($"select {Columns} from habitflow.habit_template_collections where id=@id and status='Published'", new { id }, ct);

    public async Task<IReadOnlyList<HabitTemplateCollectionItem>> ListItemsAsync(Guid collectionId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<CollectionItemWithTemplateRow>("""
            select i.collection_id,i.template_id,i.sort_order as item_sort_order,i.is_required,i.default_reminder_time,i.can_customize,
              t.id,t.objective_id,t.name,t.description,t.category,t.suggested_frequency,t.suggested_color,t.difficulty,
              t.estimated_time_minutes,t.benefit_text,t.sort_order,t.is_active,t.created_at,t.updated_at,
              coalesce((select sum(1 << d)::int from unnest(t.suggested_days) d),127) as suggested_days,
              t.suggested_target_per_week,t.suggested_reminder_time,t.icon_code,t.why_it_helps,t.how_to_start,t.first_action,
              t.tags,t.minimum_plan_code,t.is_featured,t.content_version,t.published_at
            from habitflow.habit_template_collection_items i
            join habitflow.habit_templates t on t.id=i.template_id
            where i.collection_id=@collectionId and t.is_active=true and t.published_at is not null and t.published_at<=now()
            order by i.sort_order
            """, new { collectionId }, ct);
        return rows.Select(row => new HabitTemplateCollectionItem(row.CollectionId,row.TemplateId,row.ItemSortOrder,
            row.IsRequired,row.DefaultReminderTime,row.CanCustomize,row.ToTemplate())).ToList();
    }
    private sealed record CollectionItemWithTemplateRow(Guid CollectionId, Guid TemplateId, int ItemSortOrder, bool IsRequired,
        TimeOnly? DefaultReminderTime, bool CanCustomize, Guid Id, Guid ObjectiveId, string Name, string Description,
        string Category, string SuggestedFrequency, string SuggestedColor, HabitDifficulty Difficulty, int? EstimatedTimeMinutes,
        string? BenefitText, int SortOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt, SuggestedWeekDays SuggestedDays,
        int? SuggestedTargetPerWeek, TimeOnly? SuggestedReminderTime, string? IconCode, string? WhyItHelps, string? HowToStart,
        string? FirstAction, string[]? Tags, string MinimumPlanCode, bool IsFeatured, int ContentVersion, DateTime? PublishedAt)
    {
        public HabitTemplate ToTemplate() => new(Id,ObjectiveId,Name,Description,Category,SuggestedFrequency,SuggestedColor,Difficulty,
            EstimatedTimeMinutes,BenefitText,SortOrder,IsActive,CreatedAt,UpdatedAt,SuggestedDays,SuggestedTargetPerWeek,
            SuggestedReminderTime,IconCode,WhyItHelps,HowToStart,FirstAction,Tags,MinimumPlanCode,IsFeatured,ContentVersion,PublishedAt);
    }
}
