namespace HabitFlow.Domain;

public sealed record UserReport(Guid Id, Guid UserId, string ReportType, DateOnly PeriodStart, DateOnly PeriodEnd, string Summary, DateTime CreatedAt);
