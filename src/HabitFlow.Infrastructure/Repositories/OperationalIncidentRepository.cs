using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class OperationalIncidentRepository(SqlExecutor db) : IOperationalIncidentRepository
{
    private const string IncidentColumns = """
        id as "Id",
        title as "Title",
        description as "Description",
        severity as "Severity",
        status as "Status",
        impact as "Impact",
        affected_tenants_count as "AffectedTenantsCount",
        starts_at as "StartsAt",
        estimated_resolution_at as "EstimatedResolutionAt",
        resolved_at as "ResolvedAt",
        canceled_at as "CanceledAt",
        responsible_user_id as "ResponsibleUserId",
        responsible_name as "ResponsibleName",
        communication_sent as "CommunicationSent",
        communication_notes as "CommunicationNotes",
        created_at as "CreatedAt",
        updated_at as "UpdatedAt",
        coalesce(sev_code, 'SEV3') as "SevCode",
        coalesce(affected_module, 'Geral') as "AffectedModule",
        root_cause as "RootCause",
        actions_taken as "ActionsTaken",
        next_steps as "NextSteps"
        """;

    public async Task<IReadOnlyList<OperationalIncident>> ListIncidentsAsync(OperationalIncidentFilter filter, CancellationToken ct = default)
    {
        var sql = $"select {IncidentColumns} from habitflow.operational_incidents where 1=1";
        if (!string.IsNullOrWhiteSpace(filter.Status))
            sql += " and status = @Status";
        if (!string.IsNullOrWhiteSpace(filter.Severity))
            sql += " and (severity = @Severity or sev_code = @Severity)";
        if (!string.IsNullOrWhiteSpace(filter.Search))
            sql += " and (title ilike @Like or impact ilike @Like or description ilike @Like or affected_module ilike @Like)";

        sql += " order by case when status in ('Investigating','Identified','Monitoring','Aberto','Em andamento') then 0 else 1 end, created_at desc limit 100";

        var parameters = new
        {
            filter.Status,
            filter.Severity,
            Like = string.IsNullOrWhiteSpace(filter.Search) ? null : $"%{filter.Search.Trim()}%"
        };

        return (await db.QueryAsync<OperationalIncident>(sql, parameters, ct)).ToList();
    }

    public Task<OperationalIncident?> GetIncidentByIdAsync(Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<OperationalIncident>(
            $"select {IncidentColumns} from habitflow.operational_incidents where id = @id",
            new { id }, ct);

    public async Task<Guid> CreateIncidentAsync(OperationalIncident incident, CancellationToken ct = default)
    {
        var id = incident.Id == Guid.Empty ? Guid.NewGuid() : incident.Id;
        const string sql = """
            insert into habitflow.operational_incidents (
                id, title, description, severity, status, impact, affected_tenants_count,
                starts_at, estimated_resolution_at, resolved_at, canceled_at,
                responsible_user_id, responsible_name, communication_sent, communication_notes,
                created_at, updated_at, sev_code, affected_module, root_cause, actions_taken, next_steps
            ) values (
                @id, @Title, @Description, @Severity, @Status, @Impact, @AffectedTenantsCount,
                @StartsAt, @EstimatedResolutionAt, @ResolvedAt, @CanceledAt,
                @ResponsibleUserId, @ResponsibleName, @CommunicationSent, @CommunicationNotes,
                now(), now(), @SevCode, @AffectedModule, @RootCause, @ActionsTaken, @NextSteps
            )
            """;

        await db.ExecuteAsync(sql, new
        {
            id,
            incident.Title,
            incident.Description,
            incident.Severity,
            incident.Status,
            incident.Impact,
            incident.AffectedTenantsCount,
            incident.StartsAt,
            incident.EstimatedResolutionAt,
            incident.ResolvedAt,
            incident.CanceledAt,
            incident.ResponsibleUserId,
            incident.ResponsibleName,
            incident.CommunicationSent,
            incident.CommunicationNotes,
            SevCode = string.IsNullOrWhiteSpace(incident.SevCode) ? "SEV3" : incident.SevCode,
            AffectedModule = string.IsNullOrWhiteSpace(incident.AffectedModule) ? "Geral" : incident.AffectedModule,
            incident.RootCause,
            incident.ActionsTaken,
            incident.NextSteps
        }, ct);

        return id;
    }

    public Task UpdateIncidentAsync(OperationalIncident incident, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            update habitflow.operational_incidents set
                title = @Title,
                description = @Description,
                severity = @Severity,
                status = @Status,
                impact = @Impact,
                affected_tenants_count = @AffectedTenantsCount,
                estimated_resolution_at = @EstimatedResolutionAt,
                resolved_at = @ResolvedAt,
                canceled_at = @CanceledAt,
                responsible_user_id = @ResponsibleUserId,
                responsible_name = @ResponsibleName,
                communication_sent = @CommunicationSent,
                communication_notes = @CommunicationNotes,
                sev_code = @SevCode,
                affected_module = @AffectedModule,
                root_cause = @RootCause,
                actions_taken = @ActionsTaken,
                next_steps = @NextSteps,
                updated_at = now()
            where id = @Id
            """, new
            {
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Severity,
                incident.Status,
                incident.Impact,
                incident.AffectedTenantsCount,
                incident.EstimatedResolutionAt,
                incident.ResolvedAt,
                incident.CanceledAt,
                incident.ResponsibleUserId,
                incident.ResponsibleName,
                incident.CommunicationSent,
                incident.CommunicationNotes,
                SevCode = string.IsNullOrWhiteSpace(incident.SevCode) ? "SEV3" : incident.SevCode,
                AffectedModule = string.IsNullOrWhiteSpace(incident.AffectedModule) ? "Geral" : incident.AffectedModule,
                incident.RootCause,
                incident.ActionsTaken,
                incident.NextSteps
            }, ct);

    public Task RecordIncidentAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.operational_audit_events (
                id, event_name, correlation_id, client_id, executor_user_id, executor_email,
                severity, status, payload_json, occurred_at
            ) values (
                @Id, @EventName, @CorrelationId, @ClientId, @ExecutorUserId, @ExecutorEmail,
                @Severity, @Status, @PayloadJson::jsonb, now()
            )
            """, auditEvent, ct);

    public async Task<IReadOnlyList<IncidentTenant>> ListIncidentTenantsAsync(Guid incidentId, CancellationToken ct = default) =>
        (await db.QueryAsync<IncidentTenant>("""
            select id as "Id", incident_id as "IncidentId", client_id as "ClientId",
                   impact_summary as "ImpactSummary", notified as "Notified", created_at as "CreatedAt"
            from habitflow.incident_tenants
            where incident_id = @incidentId
            """, new { incidentId }, ct)).ToList();

    public async Task LinkIncidentTenantsAsync(Guid incidentId, IEnumerable<Guid> clientIds, string? impactSummary, CancellationToken ct = default)
    {
        foreach (var clientId in clientIds)
        {
            await db.ExecuteAsync("""
                insert into habitflow.incident_tenants (id, incident_id, client_id, impact_summary, notified, created_at)
                values (gen_random_uuid(), @incidentId, @clientId, @impactSummary, false, now())
                on conflict do nothing
                """, new { incidentId, clientId, impactSummary }, ct);
        }
    }
}
