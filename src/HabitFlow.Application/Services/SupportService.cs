using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class SupportService(ISupportRepository repo, ProtocolGenerator generator, ILogger<SupportService> logger)
{
    public async Task<Result> CreateTicketAsync(User user, string title, string description, CancellationToken ct = default)
    {
        try
        {
            await repo.CreateTicketAsync(new SupportTicket(Guid.NewGuid(), user.Id, generator.Generate("SUP"), "General", TicketStatus.Open, "Normal", title, description, "web", DateTime.UtcNow, DateTime.UtcNow, null), ct);
            return Result.Success();
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar ticket para {UserId}", user.Id); return Result.Failure("support.create_error", "Não foi possível abrir o chamado."); }
    }
}
