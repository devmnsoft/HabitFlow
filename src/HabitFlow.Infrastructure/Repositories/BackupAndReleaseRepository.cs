using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class BackupAndReleaseRepository(SqlExecutor db) : IBackupRepository, IReleaseGovernanceRepository
{
    public async Task CreateRecordAsync(BackupRecord record, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.backup_records (
                id, backup_type, status, file_name, size_bytes, duration_ms,
                integrity_status, sha256_hash, notes, verified_at, created_at
            ) values (
                @Id, @BackupType, @Status, @FileName, @SizeBytes, @DurationMs,
                @IntegrityStatus, @Sha256Hash, @Notes, @VerifiedAt, @CreatedAt
            );
            """;

        await db.ExecuteAsync(sql, record, ct);
    }

    public async Task<IReadOnlyList<BackupRecord>> ListBackupsAsync(int limit = 50, CancellationToken ct = default)
    {
        const string sql = """
            select
                id as "Id",
                backup_type as "BackupType",
                status as "Status",
                file_name as "FileName",
                size_bytes as "SizeBytes",
                duration_ms as "DurationMs",
                integrity_status as "IntegrityStatus",
                sha256_hash as "Sha256Hash",
                notes as "Notes",
                verified_at as "VerifiedAt",
                created_at as "CreatedAt"
            from habitflow.backup_records
            order by created_at desc
            limit @limit;
            """;

        return (await db.QueryAsync<BackupRecord>(sql, new { limit = Math.Clamp(limit, 1, 200) }, ct)).ToList();
    }

    public async Task UpdateVerificationAsync(Guid id, string status, string integrityStatus, string? notes, CancellationToken ct = default)
    {
        const string sql = """
            update habitflow.backup_records set
                status = @status,
                integrity_status = @integrityStatus,
                notes = coalesce(@notes, notes),
                verified_at = now()
            where id = @id;
            """;

        await db.ExecuteAsync(sql, new { id, status, integrityStatus, notes }, ct);
    }

    public async Task<IReadOnlyList<ReleaseChecklistItem>> ListChecklistItemsAsync(string releaseVersion, CancellationToken ct = default)
    {
        const string sql = """
            select
                id as "Id",
                release_version as "ReleaseVersion",
                category as "Category",
                title as "Title",
                description as "Description",
                is_completed as "IsCompleted",
                completed_by as "CompletedBy",
                completed_at as "CompletedAt",
                sort_order as "SortOrder"
            from habitflow.release_checklist_items
            where release_version = @releaseVersion
            order by sort_order asc, id asc;
            """;

        return (await db.QueryAsync<ReleaseChecklistItem>(sql, new { releaseVersion }, ct)).ToList();
    }

    public async Task ToggleChecklistItemAsync(Guid id, bool isCompleted, string? completedBy, CancellationToken ct = default)
    {
        const string sql = """
            update habitflow.release_checklist_items set
                is_completed = @isCompleted,
                completed_by = @completedBy,
                completed_at = case when @isCompleted then now() else null end
            where id = @id;
            """;

        await db.ExecuteAsync(sql, new { id, isCompleted, completedBy }, ct);
    }

    public async Task SaveChecklistItemAsync(ReleaseChecklistItem item, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.release_checklist_items (
                id, release_version, category, title, description,
                is_completed, completed_by, completed_at, sort_order
            ) values (
                @Id, @ReleaseVersion, @Category, @Title, @Description,
                @IsCompleted, @CompletedBy, @CompletedAt, @SortOrder
            )
            on conflict (id) do update set
                title = excluded.title,
                description = excluded.description,
                is_completed = excluded.is_completed,
                completed_by = excluded.completed_by,
                completed_at = excluded.completed_at,
                sort_order = excluded.sort_order;
            """;

        await db.ExecuteAsync(sql, item, ct);
    }
}
