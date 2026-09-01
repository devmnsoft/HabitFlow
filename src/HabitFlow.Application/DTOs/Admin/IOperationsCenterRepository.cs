using HabitFlow.Domain;
namespace HabitFlow.Application;
public interface IOperationsCenterRepository
{
    Task<OperationsSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StructuredLogRow>> SearchLogsAsync(StructuredLogFilter filter, CancellationToken ct = default);
    Task<StructuredLogRow?> GetLogAsync(Guid id, CancellationToken ct = default);
    Task ResolveAlertAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<bool> CanConnectAsync(CancellationToken ct = default);
    Task<int> PendingMigrationsAsync(CancellationToken ct = default);
}
