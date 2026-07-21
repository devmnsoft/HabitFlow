using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class SettingsRepository(SqlExecutor db) : ISettingsRepository
{
    public Task<SystemSetting?> GetAsync(string key, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<SystemSetting>("select key, value, updated_at, updated_by from habitflow.system_settings where key=@key", new { key }, ct);
    public Task UpsertAsync(SystemSetting setting, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.system_settings(key,value,updated_at,updated_by) values(@Key,@Value::jsonb,@UpdatedAt,@UpdatedBy) on conflict(key) do update set value=@Value::jsonb,updated_at=@UpdatedAt,updated_by=@UpdatedBy", setting, ct);
    public async Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default) => (await db.QueryAsync<SystemSetting>("select key, value, updated_at, updated_by from habitflow.system_settings order by key", null, ct)).ToList();
}
