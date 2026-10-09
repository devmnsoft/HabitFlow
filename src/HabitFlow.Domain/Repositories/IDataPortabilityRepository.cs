namespace HabitFlow.Domain;

public interface IDataPortabilityRepository
{
    Task RecordExportAsync(DataExportRequestRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<DataExportRequestRecord>> ListExportsAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default);
    Task RecordImportBatchAsync(DataImportBatchRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<DataImportBatchRecord>> ListImportBatchesAsync(Guid clientId, Guid userId, int limit = 20, CancellationToken ct = default);
}
