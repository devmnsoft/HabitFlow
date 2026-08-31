using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class LgpdRepository(SqlExecutor db) : ILgpdRepository
{
    public Task CreateAsync(LgpdRequest request, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.lgpd_requests(id,user_id,protocol,type,status,notes,rejection_reason,handled_by,created_at,updated_at,completed_at) values(@Id,@UserId,@Protocol,@Type,@Status,@Notes,@RejectionReason,@HandledBy,@CreatedAt,@UpdatedAt,@CompletedAt)", new { request.Id, request.UserId, request.Protocol, Type = DbEnum.Text(request.Type), Status = DbEnum.Text(request.Status), request.Notes, request.RejectionReason, request.HandledBy, request.CreatedAt, request.UpdatedAt, request.CompletedAt }, ct);
    public async Task<IReadOnlyList<LgpdRequest>> ListByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default) => (await db.QueryAsync<LgpdRequest>("select r.id, r.user_id, r.protocol, r.type, r.status, r.notes, r.rejection_reason, r.handled_by, r.created_at, r.updated_at, r.completed_at from habitflow.lgpd_requests r join habitflow.users u on u.id=r.user_id where u.client_id=@clientId and r.user_id=@userId order by r.created_at desc", new { clientId, userId }, ct)).ToList();
    public async Task<IReadOnlyList<UserPrivacyConsent>> ListConsentsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<UserPrivacyConsent>("select c.user_id, c.consent_key, c.granted, c.updated_at from habitflow.user_privacy_consents c join habitflow.users u on u.id=c.user_id where u.client_id=@clientId and c.user_id=@userId order by c.consent_key", new { clientId, userId }, ct)).ToList();
    public Task UpsertConsentAsync(UserPrivacyConsent consent, CancellationToken ct = default) => db.ExecuteAsync(
        "insert into habitflow.user_privacy_consents(user_id,consent_key,granted,updated_at) values(@UserId,@ConsentKey,@Granted,@UpdatedAt) on conflict(user_id,consent_key) do update set granted=excluded.granted,updated_at=excluded.updated_at",
        consent, ct);

    public async Task<string> ExportOwnedDataJsonAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        await db.QuerySingleOrDefaultAsync<string>("select habitflow.export_user_data_json(@clientId,@userId)::text", new { clientId, userId }, ct) ?? "{}";

    public Task RecordSecurityEventAsync(Guid clientId, Guid userId, string eventType, string severity, CancellationToken ct = default) => db.ExecuteAsync(
        "insert into habitflow.security_audit_events(id,client_id,user_id,event_type,severity,occurred_at) values(gen_random_uuid(),@clientId,@userId,@eventType,@severity,now())",
        new { clientId, userId, eventType, severity }, ct);
}
