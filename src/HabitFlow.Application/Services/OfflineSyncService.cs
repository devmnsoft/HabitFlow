using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class OfflineSyncService(
    IOfflineSyncRepository syncRepo,
    IHabitRepository habitRepo,
    IHabitCompletionRepository completionRepo,
    AuditService audit,
    ILogger<OfflineSyncService> logger)
{
    public async Task<OfflineBatchSyncResponse> ProcessSyncBatchAsync(
        Guid clientId,
        Guid userId,
        IEnumerable<OfflineActionRequest> requests,
        CancellationToken ct = default)
    {
        var results = new List<OfflineSyncResultItem>();
        var succeeded = 0;
        var conflicts = 0;
        var failed = 0;

        foreach (var req in requests)
        {
            try
            {
                var queueItem = new OfflineSyncQueueItem(
                    req.Id,
                    clientId,
                    userId,
                    req.ActionType,
                    req.EntityId,
                    req.PayloadJson ?? "{}",
                    OfflineSyncStatus.Pending,
                    null,
                    null,
                    req.ClientCreatedAt,
                    null,
                    DateTime.UtcNow
                );

                await syncRepo.EnqueueAsync(queueItem, ct);
                await audit.LogAsync("offline.action.queued", "Ação offline enfileirada", AuditSeverity.Info, userId, null, new { req.Id, req.ActionType, clientId }, ct);

                var (status, error, conflictDetails) = await ExecuteActionAsync(clientId, userId, req, ct);

                var syncedAt = status == OfflineSyncStatus.Synced ? DateTime.UtcNow : (DateTime?)null;
                await syncRepo.UpdateStatusAsync(clientId, userId, req.Id, status, error, conflictDetails, syncedAt, ct);

                if (status == OfflineSyncStatus.Synced)
                {
                    succeeded++;
                    await audit.LogAsync("offline.action.synced", "Ação offline sincronizada com sucesso", AuditSeverity.Info, userId, null, new { req.Id, req.ActionType, clientId }, ct);
                }
                else if (status == OfflineSyncStatus.Conflict)
                {
                    conflicts++;
                    await audit.LogAsync("offline.action.conflict", "Conflito detectado na sincronização offline", AuditSeverity.Warning, userId, null, new { req.Id, req.ActionType, clientId, conflictDetails }, ct);
                }
                else
                {
                    failed++;
                }

                results.Add(new OfflineSyncResultItem(req.Id, status, error, conflictDetails, syncedAt));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar item de sincronização offline {Id}", req.Id);
                failed++;
                var errorMsg = "Erro interno ao processar sincronização.";
                await syncRepo.UpdateStatusAsync(clientId, userId, req.Id, OfflineSyncStatus.Failed, errorMsg, null, null, ct);
                results.Add(new OfflineSyncResultItem(req.Id, OfflineSyncStatus.Failed, errorMsg));
            }
        }

        return new OfflineBatchSyncResponse(results.Count, succeeded, conflicts, failed, results);
    }

    private async Task<(string Status, string? Error, string? ConflictDetails)> ExecuteActionAsync(
        Guid clientId,
        Guid userId,
        OfflineActionRequest req,
        CancellationToken ct)
    {
        switch (req.ActionType)
        {
            case OfflineActionTypes.CompleteHabit:
            {
                if (!req.EntityId.HasValue)
                    return (OfflineSyncStatus.Failed, "Identificador do hábito ausente.", null);

                var habit = await habitRepo.GetAsync(clientId, userId, req.EntityId.Value, ct);
                if (habit is null || habit.IsArchived)
                {
                    var conflict = JsonSerializer.Serialize(new { reason = "habit_not_found_or_archived", habitId = req.EntityId.Value });
                    return (OfflineSyncStatus.Conflict, "O hábito não foi encontrado ou está arquivado.", conflict);
                }

                var date = DateOnly.FromDateTime(req.ClientCreatedAt);
                var mutation = await completionRepo.AddIfMissingAsync(clientId, userId, habit.Id, date, Guid.NewGuid(), ct);
                return (OfflineSyncStatus.Synced, null, null);
            }

            case OfflineActionTypes.UndoCompletion:
            {
                if (!req.EntityId.HasValue)
                    return (OfflineSyncStatus.Failed, "Identificador do hábito ausente.", null);

                var habit = await habitRepo.GetAsync(clientId, userId, req.EntityId.Value, ct);
                if (habit is null)
                {
                    var conflict = JsonSerializer.Serialize(new { reason = "habit_not_found", habitId = req.EntityId.Value });
                    return (OfflineSyncStatus.Conflict, "O hábito não foi encontrado.", conflict);
                }

                var date = DateOnly.FromDateTime(req.ClientCreatedAt);
                await completionRepo.DeleteIfExistsAsync(clientId, userId, habit.Id, date, ct);
                return (OfflineSyncStatus.Synced, null, null);
            }

            case OfflineActionTypes.CreateDraftHabit:
            {
                if (string.IsNullOrWhiteSpace(req.PayloadJson))
                    return (OfflineSyncStatus.Failed, "Conteúdo do hábito ausente.", null);

                using var doc = JsonDocument.Parse(req.PayloadJson);
                var root = doc.RootElement;
                var name = (root.TryGetProperty("name", out var n) ? n.GetString() : null) ?? (root.TryGetProperty("Name", out var n2) ? n2.GetString() : null);
                var category = (root.TryGetProperty("category", out var c) ? c.GetString() : null) ?? (root.TryGetProperty("Category", out var c2) ? c2.GetString() : null) ?? "Geral";
                var description = (root.TryGetProperty("description", out var d) ? d.GetString() : null) ?? (root.TryGetProperty("Description", out var d2) ? d2.GetString() : null);

                if (string.IsNullOrWhiteSpace(name))
                    return (OfflineSyncStatus.Failed, "Nome do hábito é obrigatório.", null);

                var habit = new Habit(
                    req.EntityId ?? Guid.NewGuid(),
                    userId,
                    name.Trim(),
                    "#10B981",
                    category ?? "Geral",
                    false,
                    null,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    HabitFrequencyType.Daily,
                    7,
                    null,
                    description,
                    0,
                    clientId
                );

                await habitRepo.CreateAsync(habit, ct);
                return (OfflineSyncStatus.Synced, null, null);
            }

            default:
                return (OfflineSyncStatus.Failed, $"Ação offline não suportada: {req.ActionType}", null);
        }
    }
}
