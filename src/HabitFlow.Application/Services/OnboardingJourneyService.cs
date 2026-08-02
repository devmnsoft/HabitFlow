using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class OnboardingJourneyService(IUserOnboardingProgressRepository progress, IUserOnboardingDraftRepository drafts, TimeProvider clock)
{
    public Task<UserOnboardingProgress> StartAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        progress.StartOrRestartAsync(clientId, userId, ct);

    public Task<UserOnboardingProgress?> ResumeAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        progress.GetAsync(clientId, userId, ct);

    public async Task<Result<UserOnboardingProgress>> AdvanceAsync(UserOnboardingProgress next, int expectedVersion, CancellationToken ct = default)
    {
        var current = await progress.GetAsync(next.ClientId, next.UserId, ct);
        if (current is null) return Result<UserOnboardingProgress>.Failure("onboarding.not_started", "Inicie a configuração antes de continuar.");
        if (current.Version != expectedVersion) return Result<UserOnboardingProgress>.Failure("onboarding.version_conflict", "A configuração foi alterada em outra janela. Recarregue a página.");
        if (current.Status is OnboardingStatus.Completed or OnboardingStatus.Skipped)
            return Result<UserOnboardingProgress>.Failure("onboarding.terminal", "Esta configuração já foi encerrada.");
        if ((int)next.CurrentStep < (int)current.CurrentStep || (int)next.CurrentStep > (int)current.CurrentStep + 1)
            return Result<UserOnboardingProgress>.Failure("onboarding.invalid_transition", "Conclua a etapa atual antes de avançar.");
        if (!await progress.SaveAsync(next with { Version = expectedVersion + 1 }, expectedVersion, ct))
            return Result<UserOnboardingProgress>.Failure("onboarding.version_conflict", "A configuração foi alterada em outra janela. Recarregue a página.");
        return Result<UserOnboardingProgress>.Success(next with { Version = expectedVersion + 1 });
    }

    public async Task<Result<UserOnboardingProgress>> SkipAsync(Guid clientId, Guid userId, int version, CancellationToken ct = default)
    {
        var current = await progress.GetAsync(clientId,userId,ct);
        if (current is null || current.Version != version) return Result<UserOnboardingProgress>.Failure("onboarding.version_conflict", "Recarregue a página.");
        var skipped = current with { SkippedAt = clock.GetUtcNow().UtcDateTime };
        if (!await progress.SaveAsync(skipped,version,ct)) return Result<UserOnboardingProgress>.Failure("onboarding.version_conflict", "Recarregue a página.");
        await drafts.DeleteAsync(clientId,userId,ct);
        return Result<UserOnboardingProgress>.Success(skipped with { Version=version+1 });
    }
}
