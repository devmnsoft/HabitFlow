namespace HabitFlow.Domain;

public interface ISettingsRepository
{
    Task<SystemSetting?> GetAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(SystemSetting setting, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default);
}
