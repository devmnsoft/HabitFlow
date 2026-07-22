using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class LgpdRepository(SqlExecutor db) : ILgpdRepository
{
    public Task CreateAsync(LgpdRequest request, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.lgpd_requests(id,user_id,protocol,type,status,notes,rejection_reason,handled_by,created_at,updated_at,completed_at) values(@Id,@UserId,@Protocol,@Type,@Status,@Notes,@RejectionReason,@HandledBy,@CreatedAt,@UpdatedAt,@CompletedAt)", new { request.Id, request.UserId, request.Protocol, Type = DbEnum.Text(request.Type), Status = DbEnum.Text(request.Status), request.Notes, request.RejectionReason, request.HandledBy, request.CreatedAt, request.UpdatedAt, request.CompletedAt }, ct);
    public async Task<IReadOnlyList<LgpdRequest>> ListByUserAsync(Guid userId, CancellationToken ct = default) => (await db.QueryAsync<LgpdRequest>("select id, user_id, protocol, type, status, notes, rejection_reason, handled_by, created_at, updated_at, completed_at from habitflow.lgpd_requests where user_id=@userId order by created_at desc", new { userId }, ct)).ToList();
}
