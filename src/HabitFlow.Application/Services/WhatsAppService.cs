using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class WhatsAppService(WhatsAppValidator validator, ILogger<WhatsAppService> logger)
{
    public Result Validate(WhatsAppOptions options)
    {
        try { return validator.Validate(options); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao validar WhatsApp"); return Result.Failure("whatsapp.validate_error", "Não foi possível validar a configuração."); }
    }
}
