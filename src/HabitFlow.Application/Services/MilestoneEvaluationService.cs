using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class MilestoneEvaluationService(IMilestoneRepository repository)
{
    public Task<IReadOnlyList<MilestoneEvaluationResult>> EvaluateAsync(MilestoneEvaluationContext context, CancellationToken ct = default)
    {
        if (context.ClientId == Guid.Empty || context.UserId == Guid.Empty)
            throw new ArgumentException("Cliente e pessoa são obrigatórios para avaliar marcos.");
        return repository.AwardEligibleAsync(context, ct);
    }
}
