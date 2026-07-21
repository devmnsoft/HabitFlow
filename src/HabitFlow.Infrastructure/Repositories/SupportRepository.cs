using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class SupportRepository(SqlExecutor db) : ISupportRepository
{
    public Task CreateTicketAsync(SupportTicket ticket, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.support_tickets(id,user_id,protocol,type,status,priority,title,description,source,created_at,updated_at,resolved_at) values(@Id,@UserId,@Protocol,@Type,@Status::text,@Priority,@Title,@Description,@Source,@CreatedAt,@UpdatedAt,@ResolvedAt)", ticket, ct);
    public Task AddMessageAsync(SupportMessage message, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.support_messages(id,ticket_id,user_id,role,message,is_sensitive_blocked,created_at) values(@Id,@TicketId,@UserId,@Role,@Message,@IsSensitiveBlocked,@CreatedAt)", message, ct);
    public async Task<IReadOnlyList<SupportTicket>> ListByUserAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<SupportTicket>("select id, user_id, protocol, type, status, priority, title, description, source, created_at, updated_at, resolved_at from habitflow.support_tickets where user_id=@userId order by created_at desc", new { userId }, ct)).ToList();
}
