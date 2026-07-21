namespace HabitFlow.Domain;

public interface IUserUiPreferenceRepository
{
    Task<UserUiPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserUiPreference preference, CancellationToken ct = default);
}
