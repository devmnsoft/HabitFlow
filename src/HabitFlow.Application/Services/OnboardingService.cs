using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class OnboardingService(IHabitRepository habits, AuditService audit)
{
    public async Task<bool> ShouldShowOnboarding(Guid userId, CancellationToken ct = default) => await habits.CountActiveByUserAsync(userId, ct) == 0;
    public Task CompleteOnboarding(Guid userId, CancellationToken ct = default) => audit.LogAsync("onboarding_completed", "Onboarding concluído", AuditSeverity.Info, userId, null, null, ct);
    public IReadOnlyList<string> SuggestInitialHabits() => ["Beber água", "Ler 10 minutos", "Caminhar", "Estudar", "Dormir mais cedo"];
}
