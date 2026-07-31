using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class HabitTemplateFavoriteService(IHabitTemplateFavoriteRepository repository, IHabitTemplateRepository templates)
{
    public async Task<Result<bool>> SetAsync(Guid clientId, Guid userId, Guid templateId, bool favorite, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) return Result<bool>.Failure("library.tenant_required", "A conta e o usuário são obrigatórios.");
        var template = await templates.GetAsync(templateId, ct);
        if (template is not { IsActive: true } || template.PublishedAt is null) return Result<bool>.Failure("library.template_not_found", "Hábito não encontrado.");
        if (favorite) await repository.AddAsync(clientId, userId, templateId, ct); else await repository.RemoveAsync(clientId, userId, templateId, ct);
        return Result<bool>.Success(favorite);
    }

    public Task<bool> IsFavoriteAsync(Guid clientId, Guid userId, Guid templateId, CancellationToken ct = default) => repository.ExistsAsync(clientId, userId, templateId, ct);
    public Task<IReadOnlyList<HabitTemplate>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => repository.ListAsync(clientId, userId, ct);
}
