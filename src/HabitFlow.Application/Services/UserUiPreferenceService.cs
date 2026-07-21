using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class UserUiPreferenceService(IUserUiPreferenceRepository repo, AuditService audit)
{
    public UserUiPreference GetDefaultAsync(Guid userId = default) => UserUiPreference.Default(userId);

    public async Task<UserUiPreference> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await repo.GetByUserIdAsync(userId, ct);
        return existing ?? UserUiPreference.Default(userId);
    }

    public async Task<UserUiPreference> SaveAsync(Guid userId, ContrastMode contrastMode, FontScale fontScale, bool reduceMotion, bool showAchievementPopups = true, bool showTipPopups = true, bool enableToasts = true, bool reducePopups = false, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var preference = new UserUiPreference(Guid.NewGuid(), userId, contrastMode, fontScale, reduceMotion, showAchievementPopups, showTipPopups, enableToasts, reducePopups, now, now);
        await repo.UpsertAsync(preference, ct);
        await audit.LogAsync("user_ui_preferences.saved", "Preferências visuais atualizadas pelo próprio usuário.", AuditSeverity.Info, userId, metadata: new { contrastMode, fontScale, reduceMotion, showAchievementPopups, showTipPopups, enableToasts, reducePopups }, ct: ct);
        return preference;
    }
}
