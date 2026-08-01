using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class GuidedJourneyService(HabitLibraryService library, CreateHabitFromTemplateUseCase createFromTemplate, AuditService audit, ILogger<GuidedJourneyService> logger)
{
    public Task<Result<IReadOnlyList<HabitObjective>>> GetStartOptionsAsync(CancellationToken ct = default) => library.GetObjectivesAsync(ct);
    public Task<Result<IReadOnlyList<HabitObjective>>> BuildSuggestedJourneyAsync(Guid userId, CancellationToken ct = default) => library.GetObjectivesAsync(ct);

    public async Task<Result<Habit>> CompleteFirstHabitFromTemplateAsync(User user, Guid templateId, CancellationToken ct = default)
    {
        try
        {
            if (user.ClientId is null) return Result<Habit>.Failure("onboarding.tenant_required", "Selecione uma conta para continuar.");
            var template = await library.GetTemplateAsync(templateId, ct);
            if (template.IsFailure) return Result<Habit>.Failure(template.Error.Code, template.Error.Message);
            var source = template.Value!;
            var frequency = Enum.TryParse<HabitFrequencyType>(source.SuggestedFrequency, true, out var parsed) ? parsed : HabitFrequencyType.Daily;
            var selectedDays = Enumerable.Range(0, 7).Where(day => source.IsSuggestedOn((DayOfWeek)day)).ToArray();
            var result = await createFromTemplate.ExecuteAsync(new(user.ClientId.Value, user.Id, templateId, source.Name,
                frequency, source.SuggestedTargetPerWeek, selectedDays, source.SuggestedReminderTime, source.SuggestedColor,
                source.Category, null, DateOnly.FromDateTime(DateTime.UtcNow), null, false, null, null, null, false,
                "GuidedJourney", null, Guid.NewGuid(), Guid.NewGuid().ToString("N")), ct);
            if (result.IsSuccess)
            {
                await audit.LogAsync("onboarding_completed_from_template", "Onboarding concluído com hábito pronto", userId: user.Id, metadata: new { templateId }, ct: ct);
                return Result<Habit>.Success(result.Value!.Habit);
            }
            return Result<Habit>.Failure(result.Error.Code, result.Error.Message);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro no onboarding guiado de {UserId}", user.Id); return Result<Habit>.Failure("onboarding.complete_error", "Não foi possível concluir agora. Tente novamente em instantes."); }
    }
}
