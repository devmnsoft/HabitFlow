using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class LgpdGovernanceService(
    ILgpdGovernanceRepository repo,
    ILgpdRepository legacyRepo,
    IOperationalAuditRepository auditRepo,
    ILogger<LgpdGovernanceService> logger)
{
    public async Task<LgpdRequestRecord> CreateRequestAsync(
        Guid clientId,
        Guid userId,
        string requestType,
        string? reason,
        CancellationToken ct = default)
    {
        var record = new LgpdRequestRecord(
            Id: Guid.NewGuid(),
            ClientId: clientId,
            UserId: userId,
            RequestType: requestType,
            Status: LgpdRequestStatuses.Open,
            Reason: reason,
            AdminNotes: null,
            ProcessedByUserId: null,
            ProcessedAt: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        await repo.CreateRequestAsync(record, ct);

        // Registrar auditoria operacional
        var audit = new OperationalAuditEvent(
            Id: Guid.NewGuid(),
            EventName: $"lgpd.request.{requestType.ToLowerInvariant()}",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ClientId: clientId,
            ExecutorUserId: userId.ToString(),
            ExecutorEmail: null,
            Severity: "INFO",
            Status: "Created",
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                requestId = record.Id,
                requestType,
                reason = OperationalAiService.MaskSensitiveData(reason ?? string.Empty)
            }),
            OccurredAt: DateTime.UtcNow
        );
        await auditRepo.RecordAuditAsync(audit, ct);

        logger.LogInformation("Solicitação LGPD {RequestId} do tipo {Type} criada pelo usuário {UserId} no tenant {ClientId}",
            record.Id, requestType, userId, clientId);

        return record;
    }

    public Task<LgpdRequestRecord?> GetRequestByIdAsync(Guid clientId, Guid requestId, CancellationToken ct = default) =>
        repo.GetRequestByIdAsync(clientId, requestId, ct);

    public Task<IReadOnlyList<LgpdRequestRecord>> ListUserRequestsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        repo.ListRequestsByUserAsync(clientId, userId, ct);

    public Task<IReadOnlyList<LgpdRequestRecord>> ListGlobalRequestsAsync(string? status = null, int limit = 50, CancellationToken ct = default) =>
        repo.ListGlobalRequestsAsync(status, limit, ct);

    public async Task UpdateRequestStatusAsync(
        Guid requestId,
        string status,
        string? adminNotes,
        Guid processedByUserId,
        CancellationToken ct = default)
    {
        await repo.UpdateRequestStatusAsync(requestId, status, adminNotes, processedByUserId, ct);

        var audit = new OperationalAuditEvent(
            Id: Guid.NewGuid(),
            EventName: "lgpd.request.status_updated",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ClientId: null,
            ExecutorUserId: processedByUserId.ToString(),
            ExecutorEmail: null,
            Severity: "INFO",
            Status: "Updated",
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                requestId,
                newStatus = status,
                adminNotes = OperationalAiService.MaskSensitiveData(adminNotes ?? string.Empty)
            }),
            OccurredAt: DateTime.UtcNow
        );
        await auditRepo.RecordAuditAsync(audit, ct);

        logger.LogInformation("Solicitação LGPD {RequestId} atualizada para {Status} pelo admin {AdminId}",
            requestId, status, processedByUserId);
    }

    public async Task RecordConsentAsync(
        Guid clientId,
        Guid userId,
        string consentType,
        bool granted,
        string policyVersion,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var consent = new ConsentRecord(
            Id: Guid.NewGuid(),
            ClientId: clientId,
            UserId: userId,
            ConsentType: consentType,
            Granted: granted,
            PolicyVersion: policyVersion,
            IpAddress: ipAddress,
            UserAgent: userAgent,
            GrantedAt: DateTime.UtcNow,
            RevokedAt: granted ? null : DateTime.UtcNow
        );

        await repo.RecordConsentAsync(consent, ct);

        var audit = new OperationalAuditEvent(
            Id: Guid.NewGuid(),
            EventName: granted ? "consent.granted" : "consent.revoked",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ClientId: clientId,
            ExecutorUserId: userId.ToString(),
            ExecutorEmail: null,
            Severity: "INFO",
            Status: "Recorded",
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                consentType,
                granted,
                policyVersion
            }),
            OccurredAt: DateTime.UtcNow
        );
        await auditRepo.RecordAuditAsync(audit, ct);
    }

    public Task<IReadOnlyList<ConsentRecord>> ListUserConsentsAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        repo.ListConsentsByUserAsync(clientId, userId, ct);

    public async Task RevokeConsentAsync(Guid clientId, Guid userId, string consentType, CancellationToken ct = default)
    {
        await repo.RevokeConsentAsync(clientId, userId, consentType, ct);

        var audit = new OperationalAuditEvent(
            Id: Guid.NewGuid(),
            EventName: "consent.revoked",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ClientId: clientId,
            ExecutorUserId: userId.ToString(),
            ExecutorEmail: null,
            Severity: "INFO",
            Status: "Revoked",
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new { consentType }),
            OccurredAt: DateTime.UtcNow
        );
        await auditRepo.RecordAuditAsync(audit, ct);
    }

    public async Task<string> ExportUserDataJsonAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var dataJson = await legacyRepo.ExportOwnedDataJsonAsync(clientId, userId, ct);

        var audit = new OperationalAuditEvent(
            Id: Guid.NewGuid(),
            EventName: "data.exported",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ClientId: clientId,
            ExecutorUserId: userId.ToString(),
            ExecutorEmail: null,
            Severity: "INFO",
            Status: "Success",
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                exportType = "LGPD Portabilidade",
                requestedAt = DateTime.UtcNow
            }),
            OccurredAt: DateTime.UtcNow
        );
        await auditRepo.RecordAuditAsync(audit, ct);

        return dataJson;
    }
}
