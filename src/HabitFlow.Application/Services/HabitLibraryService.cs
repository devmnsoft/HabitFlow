using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitLibraryService(IHabitObjectiveRepository objectives, IHabitTemplateRepository templates, HabitService habits, AuditService audit, NotificationService notifications, ILogger<HabitLibraryService> logger)
{
    public async Task<Result<IReadOnlyList<HabitObjective>>> GetObjectivesAsync(CancellationToken ct = default)
    {
        try { await audit.LogAsync("habit_library_viewed", "Biblioteca de hábitos visualizada", ct: ct); return Result<IReadOnlyList<HabitObjective>>.Success(await objectives.ListActiveAsync(ct)); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar objetivos"); return Result<IReadOnlyList<HabitObjective>>.Failure("library.objectives_error", "Não foi possível carregar os objetivos agora."); }
    }

    public async Task<Result<IReadOnlyList<HabitTemplate>>> GetTemplatesByObjectiveAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var objective = await objectives.GetBySlugAsync(slug, ct);
            if (objective is null || !objective.IsActive) return Result<IReadOnlyList<HabitTemplate>>.Failure("library.objective_not_found", "Objetivo não encontrado.");
            await audit.LogAsync("objective_selected", "Objetivo selecionado na biblioteca", metadata: new { slug }, ct: ct);
            return Result<IReadOnlyList<HabitTemplate>>.Success(await templates.ListActiveByObjectiveAsync(objective.Id, ct));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar templates de {Slug}", slug); return Result<IReadOnlyList<HabitTemplate>>.Failure("library.templates_error", "Não foi possível carregar os hábitos prontos agora."); }
    }

    public async Task<Result<HabitTemplate>> GetTemplateAsync(Guid id, CancellationToken ct = default)
    {
        try { var template = await templates.GetAsync(id, ct); return template is { IsActive: true } ? Result<HabitTemplate>.Success(template) : Result<HabitTemplate>.Failure("library.template_inactive", "Este hábito pronto não está disponível."); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao obter template {TemplateId}", id); return Result<HabitTemplate>.Failure("library.template_error", "Não foi possível carregar este hábito pronto."); }
    }

    public async Task<Result<Habit>> AddTemplateToUserHabitsAsync(User user, Guid templateId, CancellationToken ct = default)
    {
        try
        {
            var templateResult = await GetTemplateAsync(templateId, ct);
            if (templateResult.IsFailure) return Result<Habit>.Failure(templateResult.Error.Code, templateResult.Error.Message);
            var template = templateResult.Value!;
            var created = await habits.CreateAsync(user, template.Name, template.SuggestedColor, template.Category, ct);
            if (created.IsFailure)
            {
                if (created.Error.Code.Contains("limit", StringComparison.OrdinalIgnoreCase)) await audit.LogAsync("free_limit_reached", "Limite do plano gratuito alcançado ao adicionar template", AuditSeverity.Warning, user.Id, user.Email, new { templateId }, ct);
                return created;
            }
            await audit.LogAsync("habit_template_added", "Hábito pronto adicionado ao dia do usuário", AuditSeverity.Info, user.Id, user.Email, new { templateId, template.Name }, ct);
            await notifications.CreateAsync(user.Id, "habit_template_added", "Hábito adicionado", "Hábito adicionado. Você já pode começar hoje.", "habit", created.Value!.Id, ct);
            return created;
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao adicionar template {TemplateId} para {UserId}", templateId, user.Id); return Result<Habit>.Failure("library.add_error", "Não foi possível concluir agora. Tente novamente em instantes."); }
    }
}
