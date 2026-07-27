namespace HabitFlow.Domain;
public interface IUserGoalRepository
{
 Task<IReadOnlyList<UserGoal>> ListAsync(Guid clientId, Guid userId, CancellationToken ct=default);
 Task<UserGoal?> GetAsync(Guid id, Guid clientId, Guid userId, CancellationToken ct=default);
 Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct=default);
 Task CreateAsync(UserGoal goal, CancellationToken ct=default);
 Task UpdateAsync(UserGoal goal, CancellationToken ct=default);
 Task SetStatusAsync(Guid id, Guid clientId, Guid userId, string status, CancellationToken ct=default);
 Task LinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct=default);
 Task UnlinkHabitAsync(Guid goalId, Guid habitId, Guid clientId, Guid userId, CancellationToken ct=default);
}
