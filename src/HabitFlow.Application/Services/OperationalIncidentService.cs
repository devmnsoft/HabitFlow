using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class OperationalIncidentService(
    IOperationalIncidentRepository repository,
    ILogger<OperationalIncidentService> logger)
{
    private static readonly HashSet<string> ValidSeverities = ["Info", "Minor", "Major", "Critical", "SEV1", "SEV2", "SEV3", "SEV4"];
    private static readonly HashSet<string> ValidStatuses = ["Investigating", "Identified", "Monitoring", "Resolved", "Canceled", "Aberto", "Mitigado"];

    public Task<IReadOnlyList<OperationalIncident>> ListAsync(OperationalIncidentFilter filter, CancellationToken ct = default) =>
        repository.ListIncidentsAsync(filter, ct);

    public Task<OperationalIncident?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        repository.GetIncidentByIdAsync(id, ct);

    public async Task<Result<Guid>> CreateAsync(
        string title,
        string description,
        string severity,
        string impact,
        int affectedTenantsCount,
        DateTime? estimatedResolutionAt,
        string? responsibleUserId,
        string? responsibleName,
        bool communicationSent,
        string? communicationNotes,
        string executorEmail,
        string correlationId,
        string sevCode = "SEV3",
        string affectedModule = "Geral",
        string? rootCause = null,
        string? actionsTaken = null,
        string? nextSteps = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            return Result<Guid>.Failure("incident.invalid_title", "O título do incidente deve ter no mínimo 5 caracteres.");

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10)
            return Result<Guid>.Failure("incident.invalid_description", "A descrição do incidente deve ter no mínimo 10 caracteres.");

        if (!ValidSeverities.Contains(severity))
            return Result<Guid>.Failure("incident.invalid_severity", "Severidade inválida. Use SEV1, SEV2, SEV3, SEV4, Critical, Major, Minor ou Info.");

        if (string.IsNullOrWhiteSpace(impact))
            return Result<Guid>.Failure("incident.invalid_impact", "O impacto do incidente é obrigatório.");

        var normalizedSevCode = sevCode;
        if (string.IsNullOrWhiteSpace(normalizedSevCode) || !normalizedSevCode.StartsWith("SEV", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSevCode = severity switch
            {
                "Critical" => "SEV1",
                "Major" => "SEV2",
                "Minor" => "SEV3",
                _ => "SEV4"
            };
        }

        var incident = new OperationalIncident(
            Guid.NewGuid(),
            title.Trim(),
            description.Trim(),
            severity,
            "Investigating",
            impact.Trim(),
            Math.Max(0, affectedTenantsCount),
            DateTime.UtcNow,
            estimatedResolutionAt,
            null,
            null,
            responsibleUserId,
            responsibleName,
            communicationSent,
            communicationNotes,
            DateTime.UtcNow,
            DateTime.UtcNow,
            SevCode: normalizedSevCode,
            AffectedModule: string.IsNullOrWhiteSpace(affectedModule) ? "Geral" : affectedModule.Trim(),
            RootCause: rootCause?.Trim(),
            ActionsTaken: actionsTaken?.Trim(),
            NextSteps: nextSteps?.Trim()
        );

        var id = await repository.CreateIncidentAsync(incident, ct);

        var payload = JsonSerializer.Serialize(new
        {
            incident.Id,
            incident.Title,
            incident.Severity,
            incident.Status,
            incident.AffectedTenantsCount,
            incident.ResponsibleName
        });

        await repository.RecordIncidentAuditAsync(new OperationalAuditEvent(
            Guid.NewGuid(),
            "incident.created",
            correlationId,
            null,
            responsibleUserId,
            executorEmail,
            severity == "Critical" ? "Critical" : severity == "Major" ? "Error" : "Info",
            "Success",
            payload,
            DateTime.UtcNow
        ), ct);

        logger.LogWarning(ApplicationEvents.IncidentCreated,
            "incident.created IncidentId={IncidentId} Title={Title} Severity={Severity} Status={Status} AffectedTenants={AffectedTenants} CorrelationId={CorrelationId}",
            id, incident.Title, incident.Severity, incident.Status, incident.AffectedTenantsCount, correlationId);

        return Result<Guid>.Success(id);
    }

    public async Task<Result> UpdateStatusAsync(
        Guid id,
        string newStatus,
        string? resolutionNotes,
        string executorUserId,
        string executorEmail,
        string correlationId,
        CancellationToken ct = default)
    {
        if (!ValidStatuses.Contains(newStatus))
            return Result.Failure("incident.invalid_status", "Status inválido. Use Investigating, Identified, Monitoring, Resolved ou Canceled.");

        var incident = await repository.GetIncidentByIdAsync(id, ct);
        if (incident is null)
            return Result.Failure("incident.not_found", "Incidente não encontrado.");

        DateTime? resolvedAt = incident.ResolvedAt;
        DateTime? canceledAt = incident.CanceledAt;

        if (newStatus == "Resolved" && !resolvedAt.HasValue)
            resolvedAt = DateTime.UtcNow;
        if (newStatus == "Canceled" && !canceledAt.HasValue)
            canceledAt = DateTime.UtcNow;

        var updated = incident with
        {
            Status = newStatus,
            ResolvedAt = resolvedAt,
            CanceledAt = canceledAt,
            CommunicationNotes = string.IsNullOrWhiteSpace(resolutionNotes) ? incident.CommunicationNotes : $"{incident.CommunicationNotes}\n{resolutionNotes}".Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        await repository.UpdateIncidentAsync(updated, ct);

        var isResolved = newStatus == "Resolved";
        var eventName = isResolved ? "incident.resolved" : "incident.updated";
        var eventId = isResolved ? ApplicationEvents.IncidentResolved : ApplicationEvents.IncidentUpdated;

        var payload = JsonSerializer.Serialize(new
        {
            IncidentId = id,
            OldStatus = incident.Status,
            NewStatus = newStatus,
            Notes = resolutionNotes
        });

        await repository.RecordIncidentAuditAsync(new OperationalAuditEvent(
            Guid.NewGuid(),
            eventName,
            correlationId,
            null,
            executorUserId,
            executorEmail,
            updated.Severity == "Critical" ? "Critical" : "Info",
            "Success",
            payload,
            DateTime.UtcNow
        ), ct);

        logger.LogInformation(eventId,
            "{EventName} IncidentId={IncidentId} FromStatus={FromStatus} ToStatus={ToStatus} CorrelationId={CorrelationId}",
            eventName, id, incident.Status, newStatus, correlationId);

        return Result.Success();
    }
}
