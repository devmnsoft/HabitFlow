namespace HabitFlow.Domain;

public sealed record DataExportRequestRecord(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string Scope,
    string Format,
    string Status,
    string FileName,
    int RecordCount,
    DateTime CreatedAt
);

public sealed record DataImportBatchRecord(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string EntityType,
    string Format,
    string Status,
    int TotalRows,
    int ImportedRows,
    int FailedRows,
    string ErrorsJson,
    DateTime CreatedAt
);

public sealed record HabitImportRow(
    string Name,
    string Category,
    string Frequency,
    string? Description,
    int TargetDaysPerWeek
);

public sealed record ImportSimulationResult(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<string> Errors,
    IReadOnlyList<HabitImportRow> ValidItems
);

public sealed record UserDataExportPackage(
    Guid UserId,
    string UserEmail,
    string UserName,
    DateTime ExportedAt,
    IReadOnlyList<Habit> Habits,
    IReadOnlyList<HabitCompletion> Completions,
    IReadOnlyList<UserGoal> Goals,
    IReadOnlyList<WeeklyReview> WeeklyReviews,
    IReadOnlyList<Notification> Notifications
);
