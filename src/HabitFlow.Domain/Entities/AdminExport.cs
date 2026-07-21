namespace HabitFlow.Domain;

public sealed record AdminExport(Guid Id, Guid? AdminUserId, string? AdminEmail, string ExportType, string? FileName, string? Filters, int RowsCount, DateTime CreatedAt);
