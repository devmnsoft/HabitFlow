using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class HabitTemplateCollectionRepository(SqlExecutor db, IHabitTemplateRepository templates) : IHabitTemplateCollectionRepository
{
    private const string Columns = "id,slug,name,description,objective_id,icon_code,estimated_time_minutes,difficulty,minimum_plan_code,is_featured,status,content_version,sort_order,created_at,updated_at";

    public async Task<IReadOnlyList<HabitTemplateCollection>> ListPublishedAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<HabitTemplateCollection>($"select {Columns} from habitflow.habit_template_collections where status='Published' order by sort_order,name", ct: ct)).ToList();

    public Task<HabitTemplateCollection?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<HabitTemplateCollection>($"select {Columns} from habitflow.habit_template_collections where slug=@slug and status='Published'", new { slug }, ct);

    public async Task<IReadOnlyList<HabitTemplateCollectionItem>> ListItemsAsync(Guid collectionId, CancellationToken ct = default)
    {
        var rows = await db.QueryAsync<CollectionItemRow>("select collection_id,template_id,sort_order,is_required,default_reminder_time,can_customize from habitflow.habit_template_collection_items where collection_id=@collectionId order by sort_order", new { collectionId }, ct);
        var result = new List<HabitTemplateCollectionItem>();
        foreach (var row in rows)
        {
            var template = await templates.GetAsync(row.TemplateId, ct);
            if (template is { IsActive: true, PublishedAt: not null })
                result.Add(new(row.CollectionId,row.TemplateId,row.SortOrder,row.IsRequired,row.DefaultReminderTime,row.CanCustomize,template));
        }
        return result;
    }
    private sealed record CollectionItemRow(Guid CollectionId, Guid TemplateId, int SortOrder, bool IsRequired, TimeOnly? DefaultReminderTime, bool CanCustomize);
}
