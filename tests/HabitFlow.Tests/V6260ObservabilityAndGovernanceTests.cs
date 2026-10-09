using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HabitFlow.Tests;

public class V6260ObservabilityAndGovernanceTests
{
    [Fact]
    public async Task HealthCheck_11Components_EvaluatesAllMinimalComponents()
    {
        var fakeRepo = new FakeSystemHealthRepository();
        var fakeMigrationRepo = new FakeSuperAdminOperationalRepo();
        var migService = new SchemaMigrationStatusService(fakeMigrationRepo);
        var config = new ConfigurationBuilder().Build();

        var healthService = new ObservabilityHealthService(
            fakeRepo,
            migService,
            config,
            [],
            NullLogger<ObservabilityHealthService>.Instance);

        var report = await healthService.EvaluateComprehensiveHealthAsync();

        Assert.NotNull(report);
        Assert.Equal(11, report.Components.Count);

        var expectedComponents = new[]
        {
            "Aplicação", "Banco de Dados", "Migrations", "Storage / Cache",
            "Provedores de IA", "Gateway de Pagamento", "Fila de Notificações",
            "Fila Offline e Sync", "Webhooks e Integrações", "E-mail e SMTP",
            "PWA e Service Worker"
        };

        foreach (var expected in expectedComponents)
        {
            Assert.Contains(report.Components, c => c.ComponentName == expected);
        }

        // Todos os componentes devem ter um status válido
        foreach (var comp in report.Components)
        {
            Assert.Contains(comp.Status, HealthStatusConstants.All);
            Assert.True(comp.DurationMs >= 0);
        }
    }

    [Fact]
    public async Task HealthCheck_WhenDatabaseDown_ReturnsDegradedOrUnhealthyWithSanitizedError()
    {
        var fakeRepo = new FakeSystemHealthRepository(failPing: true, simulatedError: "Connection refused: Host=10.0.0.1;Password=SuperSecret123;Username=admin");
        var fakeMigrationRepo = new FakeSuperAdminOperationalRepo();
        var migService = new SchemaMigrationStatusService(fakeMigrationRepo);
        var config = new ConfigurationBuilder().Build();

        var healthService = new ObservabilityHealthService(
            fakeRepo,
            migService,
            config,
            [],
            NullLogger<ObservabilityHealthService>.Instance);

        var report = await healthService.EvaluateComprehensiveHealthAsync();
        var dbCheck = report.Components.First(c => c.ComponentName == "Banco de Dados");

        Assert.Equal(HealthStatusConstants.Degraded, dbCheck.Status);
        // Garante que a senha e host NÃO vazaram
        Assert.DoesNotContain("SuperSecret123", dbCheck.ErrorMessage ?? "");
        Assert.DoesNotContain("SuperSecret123", dbCheck.Details ?? "");
    }

    [Fact]
    public async Task HealthCheck_WhenNoAIConfigured_ReturnsDisabledWithSafeFallback()
    {
        var fakeRepo = new FakeSystemHealthRepository();
        var fakeMigrationRepo = new FakeSuperAdminOperationalRepo();
        var migService = new SchemaMigrationStatusService(fakeMigrationRepo);
        var config = new ConfigurationBuilder().Build();

        var healthService = new ObservabilityHealthService(
            fakeRepo,
            migService,
            config,
            [], // Sem provedores configurados
            NullLogger<ObservabilityHealthService>.Instance);

        var report = await healthService.EvaluateComprehensiveHealthAsync();
        var aiCheck = report.Components.First(c => c.ComponentName == "Provedores de IA");

        Assert.Equal(HealthStatusConstants.Disabled, aiCheck.Status);
        Assert.Contains("Fallback heurístico", aiCheck.Details ?? "");
        Assert.Null(aiCheck.ErrorMessage);
    }

    [Fact]
    public async Task OperationalAudit_RecordAndSearch_FiltersByTenantAndSeverity()
    {
        var fakeAuditRepo = new FakeOperationalAuditRepository();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await fakeAuditRepo.RecordAuditAsync(new OperationalAuditEvent(
            Guid.NewGuid(), "tenant.created", "corr-1", tenantA, "admin-1", "admin@a.com", "INFO", "Success", "{}", DateTime.UtcNow));

        await fakeAuditRepo.RecordAuditAsync(new OperationalAuditEvent(
            Guid.NewGuid(), "plan.changed", "corr-2", tenantA, "admin-1", "admin@a.com", "WARNING", "Updated", "{\"plan\":\"Premium\"}", DateTime.UtcNow));

        await fakeAuditRepo.RecordAuditAsync(new OperationalAuditEvent(
            Guid.NewGuid(), "user.blocked", "corr-3", tenantB, "admin-2", "admin@b.com", "CRITICAL", "Blocked", "{}", DateTime.UtcNow));

        // Busca apenas tenant A
        var tenantAEvents = await fakeAuditRepo.SearchAuditAsync(clientId: tenantA);
        Assert.Equal(2, tenantAEvents.Count);
        Assert.All(tenantAEvents, e => Assert.Equal(tenantA, e.ClientId));

        // Busca apenas severidade CRITICAL
        var criticalEvents = await fakeAuditRepo.SearchAuditAsync(severity: "CRITICAL");
        Assert.Single(criticalEvents);
        Assert.Equal("user.blocked", criticalEvents[0].EventName);
    }

    [Fact]
    public void OperationalAudit_Payload_SanitizesSensitiveData()
    {
        var rawText = "Usuário com CPF 123.456.789-00, email dev@habitflow.com e password=SuperSecret999!";
        var masked = OperationalAiService.MaskSensitiveData(rawText);

        Assert.DoesNotContain("123.456.789-00", masked);
        Assert.Contains("123.***.***-00", masked);
        Assert.DoesNotContain("dev@habitflow.com", masked);
        Assert.Contains("[email-removido]", masked);
        Assert.DoesNotContain("SuperSecret999!", masked);
        Assert.Contains("password=[REMOVIDO]", masked);
    }

    [Fact]
    public async Task Lgpd_CreateRequest_And_ListByUser_EnforcesTenantIsolation()
    {
        var fakeLgpd = new FakeLgpdGovernanceRepository();
        var fakeLegacyLgpd = new FakeLegacyLgpdRepository();
        var fakeAudit = new FakeOperationalAuditRepository();
        var service = new LgpdGovernanceService(fakeLgpd, fakeLegacyLgpd, fakeAudit, NullLogger<LgpdGovernanceService>.Instance);

        var tenant1 = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var req1 = await service.CreateRequestAsync(tenant1, user1, LgpdRequestTypes.Export, "Quero meus dados");
        var req2 = await service.CreateRequestAsync(tenant2, user2, LgpdRequestTypes.Deletion, "Excluir conta");

        var user1Requests = await service.ListUserRequestsAsync(tenant1, user1);
        Assert.Single(user1Requests);
        Assert.Equal(req1.Id, user1Requests[0].Id);
        Assert.Equal(LgpdRequestTypes.Export, user1Requests[0].RequestType);
        Assert.Equal(LgpdRequestStatuses.Open, user1Requests[0].Status);

        // Usuário 2 no tenant 2
        var user2Requests = await service.ListUserRequestsAsync(tenant2, user2);
        Assert.Single(user2Requests);
        Assert.Equal(req2.Id, user2Requests[0].Id);

        // Atualização de status com auditoria
        await service.UpdateRequestStatusAsync(req1.Id, LgpdRequestStatuses.Processed, "Dados exportados com sucesso", Guid.NewGuid());
        var globalReqs = await service.ListGlobalRequestsAsync();
        var updated = globalReqs.First(r => r.Id == req1.Id);
        Assert.Equal(LgpdRequestStatuses.Processed, updated.Status);
    }

    [Fact]
    public async Task Lgpd_ConsentRecords_TrackGrantAndRevocation()
    {
        var fakeLgpd = new FakeLgpdGovernanceRepository();
        var fakeLegacyLgpd = new FakeLegacyLgpdRepository();
        var fakeAudit = new FakeOperationalAuditRepository();
        var service = new LgpdGovernanceService(fakeLgpd, fakeLegacyLgpd, fakeAudit, NullLogger<LgpdGovernanceService>.Instance);

        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();

        await service.RecordConsentAsync(tenant, user, "marketing_emails", true, "v2.0", "127.0.0.1", "Mozilla/5.0");
        var consents = await service.ListUserConsentsAsync(tenant, user);

        Assert.Single(consents);
        Assert.True(consents[0].Granted);
        Assert.Null(consents[0].RevokedAt);

        await service.RevokeConsentAsync(tenant, user, "marketing_emails");
        var updatedConsents = await service.ListUserConsentsAsync(tenant, user);

        Assert.Single(updatedConsents);
        Assert.False(updatedConsents[0].Granted);
        Assert.NotNull(updatedConsents[0].RevokedAt);
    }

    [Fact]
    public async Task BackupGovernance_TargetsAndRestoreChecklist_ValidatesRpoRto()
    {
        var fakeBackup = new FakeBackupRepository();
        var fakeRelease = new FakeReleaseGovernanceRepository();
        var fakeHealth = new ObservabilityHealthService(new FakeSystemHealthRepository(), new SchemaMigrationStatusService(new FakeSuperAdminOperationalRepo()), new ConfigurationBuilder().Build(), [], NullLogger<ObservabilityHealthService>.Instance);
        var service = new BackupAndReleaseGovernanceService(fakeBackup, fakeRelease, new SchemaMigrationStatusService(new FakeSuperAdminOperationalRepo()), fakeHealth, NullLogger<BackupAndReleaseGovernanceService>.Instance);

        var info = await service.GetBackupGovernanceInfoAsync();

        Assert.NotNull(info);
        Assert.Contains("≤ 1 hora", info.RpoTarget);
        Assert.Contains("≤ 30 minutos", info.RtoTarget);
        Assert.True(info.RestoreChecklistSteps.Count >= 5);
        Assert.Contains(info.RestoreChecklistSteps, s => s.Contains("descartável", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReleaseGovernance_ChecklistAndVersion_TracksReadiness()
    {
        var fakeBackup = new FakeBackupRepository();
        var fakeRelease = new FakeReleaseGovernanceRepository();
        var fakeHealth = new ObservabilityHealthService(new FakeSystemHealthRepository(), new SchemaMigrationStatusService(new FakeSuperAdminOperationalRepo()), new ConfigurationBuilder().Build(), [], NullLogger<ObservabilityHealthService>.Instance);
        var service = new BackupAndReleaseGovernanceService(fakeBackup, fakeRelease, new SchemaMigrationStatusService(new FakeSuperAdminOperationalRepo()), fakeHealth, NullLogger<BackupAndReleaseGovernanceService>.Instance);

        var status = await service.GetReleaseStatusAsync("v6.26.0");

        Assert.Equal("v6.26.0", status.CurrentVersion);
        Assert.True(status.Checklist.Count >= 5);

        var item = status.Checklist.First(i => !i.IsCompleted);
        await service.ToggleChecklistItemAsync(item.Id, true, "DevOpsLead");

        var updatedStatus = await service.GetReleaseStatusAsync("v6.26.0");
        var updatedItem = updatedStatus.Checklist.First(i => i.Id == item.Id);
        Assert.True(updatedItem.IsCompleted);
        Assert.Equal("DevOpsLead", updatedItem.CompletedBy);
    }

    [Fact]
    public async Task IncidentManagement_SevClassification_SupportsSev1ToSev4AndModule()
    {
        var fakeRepo = new FakeIncidentRepository();
        var service = new OperationalIncidentService(fakeRepo, NullLogger<OperationalIncidentService>.Instance);

        var result = await service.CreateAsync(
            title: "Instabilidade Crítica no Gateway",
            description: "Latência elevada nas requisições do webhook",
            severity: "Critical",
            impact: "Atraso na confirmação de faturas",
            affectedTenantsCount: 15,
            estimatedResolutionAt: DateTime.UtcNow.AddHours(1),
            responsibleUserId: "usr-1",
            responsibleName: "SRE OnCall",
            communicationSent: false,
            communicationNotes: "Investigando conexões",
            executorEmail: "sre@habitflow.com",
            correlationId: "corr-sev1",
            sevCode: "SEV1",
            affectedModule: "Pagamentos",
            rootCause: "Pool de conexões exaurido",
            actionsTaken: "Escalado pod e reiniciado pool",
            nextSteps: "Revisar max_connections do PostgreSQL"
        );

        Assert.True(result.IsSuccess);
        var incident = await fakeRepo.GetIncidentByIdAsync(result.Value);
        Assert.NotNull(incident);
        Assert.Equal("SEV1", incident.SevCode);
        Assert.Equal("Pagamentos", incident.AffectedModule);
        Assert.Equal("Pool de conexões exaurido", incident.RootCause);
        Assert.Equal("Escalado pod e reiniciado pool", incident.ActionsTaken);
        Assert.Equal("Revisar max_connections do PostgreSQL", incident.NextSteps);
    }

    [Fact]
    public async Task OperationalAi_WhenDisabled_ProvidesDeterministicSafeFallback()
    {
        var aiService = new OperationalAiService([], new AssistantSafetyPolicy(), NullLogger<OperationalAiService>.Instance);

        // 1. Root Cause
        var rootCause = await aiService.ExplainIncidentRootCauseAsync(
            "Falha de sincronização", "OfflineSync", "SEV2", "Eventos acumulados sem reconciliação");
        Assert.NotNull(rootCause.Suggestion);
        Assert.Equal("LocalGuard", rootCause.Provider);
        Assert.Contains("OfflineSync", rootCause.Suggestion);

        // 2. Health Summary
        var healthSummary = await aiService.SummarizeHealthChecksAsync([
            new SystemHealthStatusItem("Banco de Dados", HealthStatusConstants.Healthy, 2, null, null, null, DateTime.UtcNow),
            new SystemHealthStatusItem("IA", HealthStatusConstants.Disabled, 0, null, null, null, DateTime.UtcNow)
        ]);
        Assert.NotNull(healthSummary.Suggestion);
        Assert.Contains("SISTEMA ESTÁVEL", healthSummary.Suggestion);

        // 3. Release Summary
        var releaseSummary = await aiService.GenerateReleaseSummaryAsync("v6.26.0", [
            new ReleaseChecklistItem(Guid.NewGuid(), "v6.26.0", "BD", "Migrations", null, true, "SRE", DateTime.UtcNow, 1),
            new ReleaseChecklistItem(Guid.NewGuid(), "v6.26.0", "QA", "Testes", null, true, "QA", DateTime.UtcNow, 2)
        ]);
        Assert.NotNull(releaseSummary.Suggestion);
        Assert.Contains("v6.26.0", releaseSummary.Suggestion);
    }
}

#region Fakes para Testes Unitários de Observabilidade e Governança

internal sealed class FakeSystemHealthRepository(bool failPing = false, string? simulatedError = null) : ISystemHealthRepository
{
    private readonly List<SystemHealthStatusItem> _items = [];
    private readonly List<(string Component, string Status, int Duration, string? Error, DateTime Time)> _history = [];

    public Task<bool> PingDatabaseAsync(CancellationToken ct = default)
    {
        if (failPing) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public Task SaveHealthCheckAsync(SystemHealthStatusItem item, CancellationToken ct = default)
    {
        _items.RemoveAll(i => i.ComponentName == item.ComponentName);
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SystemHealthStatusItem>> GetLatestHealthChecksAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SystemHealthStatusItem>>(_items.ToList());

    public Task RecordHistoryAsync(string componentName, string status, int durationMs, string? error, CancellationToken ct = default)
    {
        _history.Add((componentName, status, durationMs, error, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SystemHealthStatusItem>> GetHealthHistoryAsync(string? componentName = null, int limit = 50, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SystemHealthStatusItem>>([]);
}

internal sealed class FakeSuperAdminOperationalRepo : ISuperAdminOperationalRepository
{
    public Task<SystemHealthStatus> BuildSystemHealthAsync(IReadOnlyList<SchemaMigrationStatus> expected, CancellationToken ct = default) =>
        Task.FromResult(new SystemHealthStatus(true, expected, [], [], [], 5, 10, false, "Test", "v6.26.0", "http://localhost", false));

    public Task<IReadOnlyList<SuperAdminPlanRow>> ListPlansAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SuperAdminPlanRow>>([]);
    public Task<IReadOnlyList<SuperAdminSubscriptionRow>> ListSubscriptionsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SuperAdminSubscriptionRow>>([]);
    public Task<IReadOnlyList<SuperAdminPaymentRow>> ListPaymentsAsync(string? status = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SuperAdminPaymentRow>>([]);
    public Task<IReadOnlyList<SuperAdminAuditRow>> ListAuditAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SuperAdminAuditRow>>([]);
    public Task<IReadOnlyList<SchemaMigrationStatus>> ListAppliedMigrationsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SchemaMigrationStatus>>([new SchemaMigrationStatus("101", "v6260_production_observability_lgpd_backup_incidents_governance", true, DateTime.UtcNow)]);

    public Task ApplyActionAsync(string action, string targetType, Guid? targetId, string reason, string actorEmail, object? metadata = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task ChangeClientPlanAsync(Guid clientId, string planCode, string reason, string actorEmail, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkInvoicePaidAsync(Guid invoiceId, string reason, string actorEmail, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkInvoiceOverdueAsync(Guid invoiceId, string reason, string actorEmail, CancellationToken ct = default) => Task.CompletedTask;
    public Task CancelSubscriptionAsync(Guid subscriptionId, string reason, string actorEmail, CancellationToken ct = default) => Task.CompletedTask;
    public Task ReactivateSubscriptionAsync(Guid subscriptionId, string reason, string actorEmail, CancellationToken ct = default) => Task.CompletedTask;
    public Task<RegistrationQualityReport> GetRegistrationQualityAsync(CancellationToken ct = default) =>
        Task.FromResult(new RegistrationQualityReport(new RegistrationQualitySummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), []));
}

internal sealed class FakeIncidentRepository : IOperationalIncidentRepository
{
    private readonly List<OperationalIncident> _incidents = [];

    public Task<IReadOnlyList<OperationalIncident>> ListIncidentsAsync(OperationalIncidentFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OperationalIncident>>(_incidents.ToList());

    public Task<OperationalIncident?> GetIncidentByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_incidents.FirstOrDefault(i => i.Id == id));

    public Task<Guid> CreateIncidentAsync(OperationalIncident incident, CancellationToken ct = default)
    {
        _incidents.Add(incident);
        return Task.FromResult(incident.Id);
    }

    public Task UpdateIncidentAsync(OperationalIncident incident, CancellationToken ct = default)
    {
        var idx = _incidents.FindIndex(i => i.Id == incident.Id);
        if (idx >= 0) _incidents[idx] = incident;
        return Task.CompletedTask;
    }

    public Task RecordIncidentAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<IncidentTenant>> ListIncidentTenantsAsync(Guid incidentId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<IncidentTenant>>([]);
    public Task LinkIncidentTenantsAsync(Guid incidentId, IEnumerable<Guid> clientIds, string? impactSummary, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeOperationalAuditRepository : IOperationalAuditRepository
{
    private readonly List<OperationalAuditEvent> _events = [];

    public Task RecordAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OperationalAuditEvent>> SearchAuditAsync(
        Guid? clientId = null,
        string? executorUserId = null,
        string? eventName = null,
        string? severity = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        IEnumerable<OperationalAuditEvent> query = _events;

        if (clientId.HasValue)
            query = query.Where(e => e.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => string.Equals(e.Severity, severity, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(eventName))
            query = query.Where(e => e.EventName.Contains(eventName, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<IReadOnlyList<OperationalAuditEvent>>(query.Take(limit).ToList());
    }
}

internal sealed class FakeLgpdGovernanceRepository : ILgpdGovernanceRepository
{
    private readonly List<LgpdRequestRecord> _requests = [];
    private readonly List<ConsentRecord> _consents = [];

    public Task CreateRequestAsync(LgpdRequestRecord request, CancellationToken ct = default)
    {
        _requests.Add(request);
        return Task.CompletedTask;
    }

    public Task<LgpdRequestRecord?> GetRequestByIdAsync(Guid clientId, Guid requestId, CancellationToken ct = default) =>
        Task.FromResult(_requests.FirstOrDefault(r => r.Id == requestId));

    public Task<IReadOnlyList<LgpdRequestRecord>> ListRequestsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<LgpdRequestRecord>>(_requests.Where(r => r.ClientId == clientId && r.UserId == userId).ToList());

    public Task<IReadOnlyList<LgpdRequestRecord>> ListGlobalRequestsAsync(string? status = null, int limit = 50, CancellationToken ct = default)
    {
        IEnumerable<LgpdRequestRecord> q = _requests;
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);
        return Task.FromResult<IReadOnlyList<LgpdRequestRecord>>(q.Take(limit).ToList());
    }

    public Task UpdateRequestStatusAsync(Guid requestId, string status, string? adminNotes, Guid processedByUserId, CancellationToken ct = default)
    {
        var idx = _requests.FindIndex(r => r.Id == requestId);
        if (idx >= 0)
        {
            var cur = _requests[idx];
            _requests[idx] = cur with { Status = status, AdminNotes = adminNotes, ProcessedByUserId = processedByUserId, ProcessedAt = DateTime.UtcNow };
        }
        return Task.CompletedTask;
    }

    public Task RecordConsentAsync(ConsentRecord consent, CancellationToken ct = default)
    {
        _consents.RemoveAll(c => c.ClientId == consent.ClientId && c.UserId == consent.UserId && c.ConsentType == consent.ConsentType);
        _consents.Add(consent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConsentRecord>> ListConsentsByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ConsentRecord>>(_consents.Where(c => c.ClientId == clientId && c.UserId == userId).ToList());

    public Task RevokeConsentAsync(Guid clientId, Guid userId, string consentType, CancellationToken ct = default)
    {
        var idx = _consents.FindIndex(c => c.ClientId == clientId && c.UserId == userId && c.ConsentType == consentType);
        if (idx >= 0)
        {
            var cur = _consents[idx];
            _consents[idx] = cur with { Granted = false, RevokedAt = DateTime.UtcNow };
        }
        return Task.CompletedTask;
    }
}

internal sealed class FakeLegacyLgpdRepository : ILgpdRepository
{
    public Task CreateAsync(LgpdRequest request, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<LgpdRequest>> ListByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<LgpdRequest>>([]);
    public Task<IReadOnlyList<UserPrivacyConsent>> ListConsentsAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserPrivacyConsent>>([]);
    public Task UpsertConsentAsync(UserPrivacyConsent consent, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> ExportOwnedDataJsonAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult("{\"user\":{\"habits\":[]}}");
    public Task RecordSecurityEventAsync(Guid clientId, Guid userId, string eventType, string severity, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeBackupRepository : IBackupRepository
{
    private readonly List<BackupRecord> _records = [];

    public Task CreateRecordAsync(BackupRecord record, CancellationToken ct = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BackupRecord>> ListBackupsAsync(int limit = 50, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BackupRecord>>(_records.Take(limit).ToList());

    public Task UpdateVerificationAsync(Guid id, string status, string integrityStatus, string? notes, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeReleaseGovernanceRepository : IReleaseGovernanceRepository
{
    private readonly List<ReleaseChecklistItem> _items = [];

    public Task<IReadOnlyList<ReleaseChecklistItem>> ListChecklistItemsAsync(string releaseVersion, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ReleaseChecklistItem>>(_items.Where(i => i.ReleaseVersion == releaseVersion).ToList());

    public Task ToggleChecklistItemAsync(Guid id, bool isCompleted, string? completedBy, CancellationToken ct = default)
    {
        var idx = _items.FindIndex(i => i.Id == id);
        if (idx >= 0)
        {
            var cur = _items[idx];
            _items[idx] = cur with { IsCompleted = isCompleted, CompletedBy = completedBy, CompletedAt = isCompleted ? DateTime.UtcNow : null };
        }
        return Task.CompletedTask;
    }

    public Task SaveChecklistItemAsync(ReleaseChecklistItem item, CancellationToken ct = default)
    {
        _items.RemoveAll(i => i.Id == item.Id);
        _items.Add(item);
        return Task.CompletedTask;
    }
}

#endregion
