using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ProductTipRepository(SqlExecutor db) : IProductTipRepository
{
    public Task<ProductTip?> GetNextAsync(Guid clientId, Guid userId, string path, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<ProductTip>("""
            select p.id,p.code,p.route_pattern,p.target_selector,p.title,p.content,p.display_order,p.is_active
              from habitflow.product_tips p
              join habitflow.users u on u.id=@userId and u.client_id=@clientId
              left join habitflow.user_product_tips up on up.product_tip_id=p.id and up.user_id=u.id
             where p.is_active=true and @path like p.route_pattern and up.dismissed_at is null
             order by p.display_order,p.code limit 1
            """, new { clientId, userId, path }, ct);

    public async Task<bool> DismissAsync(Guid clientId, Guid userId, Guid tipId, DateTime occurredAt, CancellationToken ct = default) =>
        await db.ExecuteAsync("""
            insert into habitflow.user_product_tips(user_id,product_tip_id,seen_at,dismissed_at,updated_at)
            select u.id,p.id,@occurredAt,@occurredAt,@occurredAt from habitflow.users u
            cross join habitflow.product_tips p where u.id=@userId and u.client_id=@clientId and p.id=@tipId
            on conflict(user_id,product_tip_id) do update set seen_at=coalesce(habitflow.user_product_tips.seen_at,@occurredAt),dismissed_at=@occurredAt,updated_at=@occurredAt
            """, new { clientId, userId, tipId, occurredAt }, ct) == 1;

    public Task<int> ReopenAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            delete from habitflow.user_product_tips up using habitflow.users u
             where up.user_id=u.id and u.id=@userId and u.client_id=@clientId
            """, new { clientId, userId }, ct);
}

