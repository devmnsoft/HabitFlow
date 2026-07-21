namespace HabitFlow.Domain;

public sealed record UserUiPreference(Guid Id, Guid UserId, ContrastMode ContrastMode, FontScale FontScale, bool ReduceMotion, bool ShowAchievementPopups, bool ShowTipPopups, bool EnableToasts, bool ReducePopups, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static UserUiPreference Default(Guid userId) => new(Guid.Empty, userId, ContrastMode.Default, FontScale.Normal, false, true, true, true, false, DateTime.UtcNow, DateTime.UtcNow);
}
