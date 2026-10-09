using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class LgpdGovernanceRepository(SqlExecutor db) : ILgpdGovernanceRepository
{
    public async Task CreateRequestAsync(LgpdRequestRecord request, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.lgpd_requests (
                id, client_id, user_id, request_type, status, reason, admin_notes,
                processed_by_user_id, processed_at, created_at, updated_at
            ) values (
                @Id, @ClientId, @UserId, @RequestType, @Status, @Reason, @AdminNotes,
                @ProcessedByUserId, @ProcessedAt, @CreatedAt, @UpdatedAt
            );
            """;

        await db.ExecuteAsync(sql, request, ct);
    }

    public async Task<LgpdRequestRecord?> GetRequestByIdAsync(Guid clientId, Guid requestId, CancellationToken ct = default)
    {
        const string sql = """
            select
                id as "Id",
                client_id as "ClientId",
                user_id as "UserId",
                request_type as "RequestType",
                status as "Status",
                reason as "Reason",
                admin_notes as "AdminNotes",
                processed_by_user_id as "ProcessedByUserId",
                processed_at as "ProcessedAt",
                created_at as "CreatedAt",
                updated_at as "UpdatedAt"
            from habitflow.lgpd_requests
            where id = @requestId and (client_id = @clientId or @clientId = '00000000-0000-0000-0000-000000000000'::uuid);
            """;

        return await db.QuerySingleOrDefaultAsync<LgpdRequestRecord>(sql, new { clientId, requestId }, ct);
    }

    public async Task<IReadOnlyList<LgpdRequestRecord>> ListRequestsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select
                id as "Id",
                client_id as "ClientId",
                user_id as "UserId",
                request_type as "RequestType",
                status as "Status",
                reason as "Reason",
                admin_notes as "AdminNotes",
                processed_by_user_id as "ProcessedByUserId",
                processed_at as "ProcessedAt",
                created_at as "CreatedAt",
                updated_at as "UpdatedAt"
            from habitflow.lgpd_requests
            where client_id = @clientId and user_id = @userId
            order by created_at desc;
            """;

        return (await db.QueryAsync<LgpdRequestRecord>(sql, new { clientId, userId }, ct)).ToList();
    }

    public async Task<IReadOnlyList<LgpdRequestRecord>> ListGlobalRequestsAsync(string? status = null, int limit = 50, CancellationToken ct = default)
    {
        var sql = """
            select
                id as "Id",
                client_id as "ClientId",
                user_id as "UserId",
                request_type as "RequestType",
                status as "Status",
                reason as "Reason",
                admin_notes as "AdminNotes",
                processed_by_user_id as "ProcessedByUserId",
                processed_at as "ProcessedAt",
                created_at as "CreatedAt",
                updated_at as "UpdatedAt"
            from habitflow.lgpd_requests
            """;

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " where status = @status";
        }

        sql += " order by created_at desc limit @limit";

        return (await db.QueryAsync<LgpdRequestRecord>(sql, new { status, limit = Math.Clamp(limit, 1, 200) }, ct)).ToList();
    }

    public async Task UpdateRequestStatusAsync(Guid requestId, string status, string? adminNotes, Guid processedByUserId, CancellationToken ct = default)
    {
        const string sql = """
            update habitflow.lgpd_requests set
                status = @status,
                admin_notes = @adminNotes,
                processed_by_user_id = @processedByUserId,
                processed_at = now(),
                updated_at = now()
            where id = @requestId;
            """;

        await db.ExecuteAsync(sql, new { requestId, status, adminNotes, processedByUserId }, ct);
    }

    public async Task RecordConsentAsync(ConsentRecord consent, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.consent_records (
                id, client_id, user_id, consent_type, granted, policy_version,
                ip_address, user_agent, granted_at, revoked_at
            ) values (
                @Id, @ClientId, @UserId, @ConsentType, @Granted, @PolicyVersion,
                @IpAddress, @UserAgent, @GrantedAt, @RevokedAt
            );
            """;

        await db.ExecuteAsync(sql, consent, ct);
    }

    public async Task<IReadOnlyList<ConsentRecord>> ListConsentsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            select
                id as "Id",
                client_id as "ClientId",
                user_id as "UserId",
                consent_type as "ConsentType",
                granted as "Granted",
                policy_version as "PolicyVersion",
                ip_address as "IpAddress",
                user_agent as "UserAgent",
                granted_at as "GrantedAt",
                revoked_at as "RevokedAt"
            from habitflow.consent_records
            where client_id = @clientId and user_id = @userId
            order by granted_at desc;
            """;

        return (await db.QueryAsync<ConsentRecord>(sql, new { clientId, userId }, ct)).ToList();
    }

    public async Task RevokeConsentAsync(Guid clientId, Guid userId, string consentType, CancellationToken ct = default)
    {
        const string sql = """
            update habitflow.consent_records set
                granted = false,
                revoked_at = now()
            where client_id = @clientId and user_id = @userId and consent_type = @consentType and granted = true;
            """;

        await db.ExecuteAsync(sql, new { clientId, userId, consentType }, ct);
    }
}
