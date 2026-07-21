namespace HabitFlow.Domain;

public sealed record UserUiPreference(Guid Id, Guid UserId, ContrastMode ContrastMode, FontScale FontScale, bool ReduceMotion, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static UserUiPreference Default(Guid userId) => new(Guid.Empty, userId, ContrastMode.Default, FontScale.Normal, false, DateTime.UtcNow, DateTime.UtcNow);
}
