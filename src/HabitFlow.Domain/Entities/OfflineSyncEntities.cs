namespace HabitFlow.Domain;

public static class OfflineSyncStatus
{
    public const string Pending = "Pending";
    public const string Synced = "Synced";
    public const string Failed = "Failed";
    public const string Conflict = "Conflict";
    public const string Discarded = "Discarded";

    public static readonly IReadOnlyList<string> All = [Pending, Synced, Failed, Conflict, Discarded];
}

public static class OfflineActionTypes
{
    public const string CompleteHabit = "complete_habit";
    public const string UndoCompletion = "undo_completion";
    public const string CreateDraftHabit = "create_draft_habit";
}

public sealed record OfflineSyncQueueItem(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string ActionType,
    Guid? EntityId,
    string PayloadJson,
    string Status,
    string? ErrorMessage,
    string? ConflictDetailsJson,
    DateTime ClientCreatedAt,
    DateTime? SyncedAt,
    DateTime CreatedAt
);

public sealed record OfflineActionRequest(
    Guid Id,
    string ActionType,
    Guid? EntityId,
    string? PayloadJson,
    DateTime ClientCreatedAt
);

public sealed record OfflineSyncResultItem(
    Guid Id,
    string Status,
    string? ErrorMessage = null,
    string? ConflictDetailsJson = null,
    DateTime? SyncedAt = null
);

public sealed record OfflineBatchSyncResponse(
    int TotalProcessed,
    int Succeeded,
    int Conflicts,
    int Failed,
    IReadOnlyList<OfflineSyncResultItem> Results
);
