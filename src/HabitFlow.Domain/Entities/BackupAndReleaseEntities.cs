namespace HabitFlow.Domain;

public sealed record BackupRecord(
    Guid Id,
    string BackupType,
    string Status,
    string FileName,
    long SizeBytes,
    int DurationMs,
    string IntegrityStatus,
    string? Sha256Hash,
    string? Notes,
    DateTime? VerifiedAt,
    DateTime CreatedAt
);

public interface IBackupRepository
{
    Task CreateRecordAsync(BackupRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<BackupRecord>> ListBackupsAsync(int limit = 50, CancellationToken ct = default);
    Task UpdateVerificationAsync(Guid id, string status, string integrityStatus, string? notes, CancellationToken ct = default);
}

public sealed record ReleaseChecklistItem(
    Guid Id,
    string ReleaseVersion,
    string Category,
    string Title,
    string? Description,
    bool IsCompleted,
    string? CompletedBy,
    DateTime? CompletedAt,
    int SortOrder
);

public interface IReleaseGovernanceRepository
{
    Task<IReadOnlyList<ReleaseChecklistItem>> ListChecklistItemsAsync(string releaseVersion, CancellationToken ct = default);
    Task ToggleChecklistItemAsync(Guid id, bool isCompleted, string? completedBy, CancellationToken ct = default);
    Task SaveChecklistItemAsync(ReleaseChecklistItem item, CancellationToken ct = default);
}
