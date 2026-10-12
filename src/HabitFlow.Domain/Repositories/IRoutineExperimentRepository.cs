namespace HabitFlow.Domain;

public interface IRoutineExperimentRepository
{
    Task<RoutineExperiment?> GetAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoutineExperiment>> ListByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task<IReadOnlyList<RoutineExperiment>> ListActiveForHabitAsync(Guid clientId, Guid userId, Guid habitId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task CreateAsync(RoutineExperiment experiment, CancellationToken ct = default);
    Task<bool> UpdateAsync(RoutineExperiment experiment, int expectedVersion, CancellationToken ct = default);
    Task AddEventAsync(RoutineExperimentEvent value, CancellationToken ct = default);
    Task AddLearningAsync(RoutineExperimentLearning value, CancellationToken ct = default);
    Task<IReadOnlyList<RoutineExperimentLearning>> ListLearningsByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
}
