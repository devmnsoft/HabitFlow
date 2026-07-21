using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class LgpdService(ILgpdRepository repo, ProtocolGenerator generator, ILogger<LgpdService> logger)
{
    public async Task<Result> RequestAsync(User user, LgpdRequestType type, CancellationToken ct = default)
    {
        try { await repo.CreateAsync(new LgpdRequest(Guid.NewGuid(), user.Id, generator.Generate("LGPD"), type, LgpdRequestStatus.Requested, null, null, null, DateTime.UtcNow, DateTime.UtcNow, null), ct); return Result.Success(); }
        catch (Exception ex) { logger.LogError(ex, "Erro LGPD para {UserId}", user.Id); return Result.Failure("lgpd.request_error", "Não foi possível registrar a solicitação LGPD."); }
    }
}
