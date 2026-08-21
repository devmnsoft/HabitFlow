namespace HabitFlow.Domain;

public sealed record UserReport(Guid Id, Guid ClientId, Guid UserId, string ReportType, DateOnly PeriodStart, DateOnly PeriodEnd,
    string Summary, int AlgorithmVersion, DateTime CreatedAt);
