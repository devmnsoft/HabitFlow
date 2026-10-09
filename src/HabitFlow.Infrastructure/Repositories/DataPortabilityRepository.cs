using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class DataPortabilityRepository(SqlExecutor db) : IDataPortabilityRepository
{
    public Task RecordExportAsync(DataExportRequestRecord record, CancellationToken ct = default) =>
        db.ExecuteAsync(
            "insert into habitflow.data_export_requests(id,client_id,user_id,scope,format,status,file_name,record_count,created_at) " +
            "values(@Id,@ClientId,@UserId,@Scope,@Format,@Status,@FileName,@RecordCount,@CreatedAt)",
            record, ct);

    public async Task<IReadOnlyList<DataExportRequestRecord>> ListExportsAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default) =>
        (await db.QueryAsync<DataExportRequestRecord>(
            "select id,client_id,user_id,scope,format,status,file_name,record_count,created_at " +
            "from habitflow.data_export_requests where client_id=@clientId and user_id=@userId order by created_at desc limit @limit",
            new { clientId, userId, limit }, ct)).ToList();

    public Task RecordImportBatchAsync(DataImportBatchRecord record, CancellationToken ct = default) =>
        db.ExecuteAsync(
            "insert into habitflow.data_import_batches(id,client_id,user_id,entity_type,format,status,total_rows,imported_rows,failed_rows,errors_json,created_at) " +
            "values(@Id,@ClientId,@UserId,@EntityType,@Format,@Status,@TotalRows,@ImportedRows,@FailedRows,cast(@ErrorsJson as jsonb),@CreatedAt)",
            record, ct);

    public async Task<IReadOnlyList<DataImportBatchRecord>> ListImportBatchesAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default) =>
        (await db.QueryAsync<DataImportBatchRecord>(
            "select id,client_id,user_id,entity_type,format,status,total_rows,imported_rows,failed_rows,errors_json::text as errors_json,created_at " +
            "from habitflow.data_import_batches where client_id=@clientId and user_id=@userId order by created_at desc limit @limit",
            new { clientId, userId, limit }, ct)).ToList();
}
