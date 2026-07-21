using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class GuidedJourneyService(HabitLibraryService library, IAuditRepository auditRepo, AuditService audit, ILogger<GuidedJourneyService> logger)
{
    public Task<Result<IReadOnlyList<HabitObjective>>> GetStartOptionsAsync(CancellationToken ct = default) => library.GetObjectivesAsync(ct);
    public Task<Result<IReadOnlyList<HabitObjective>>> BuildSuggestedJourneyAsync(Guid userId, CancellationToken ct = default) => library.GetObjectivesAsync(ct);

    public async Task<Result<Habit>> CompleteFirstHabitFromTemplateAsync(User user, Guid templateId, CancellationToken ct = default)
    {
        try
        {
            var result = await library.AddTemplateToUserHabitsAsync(user, templateId, ct);
            if (result.IsSuccess) await audit.LogAsync("onboarding_completed_from_template", "Onboarding concluído com hábito pronto", AuditSeverity.Info, user.Id, user.Email, new { templateId }, ct);
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Erro no onboarding guiado de {UserId}", user.Id); return Result<Habit>.Failure("onboarding.complete_error", "Não foi possível concluir agora. Tente novamente em instantes."); }
    }
}
