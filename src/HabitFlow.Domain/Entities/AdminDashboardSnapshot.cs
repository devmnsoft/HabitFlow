namespace HabitFlow.Domain;

public sealed record AdminDashboardSnapshot(Guid Id, DateOnly SnapshotDate, string Metrics, DateTime CreatedAt);
