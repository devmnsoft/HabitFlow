using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class SettingsService(ISettingsRepository repo, ILogger<SettingsService> logger)
{
    public async Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default)
    {
        try { return await repo.ListAsync(ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar configurações"); return Array.Empty<SystemSetting>(); }
    }
}
