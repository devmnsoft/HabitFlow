namespace HabitFlow.Domain;

public sealed record SystemSetting(string Key, string Value, DateTime UpdatedAt, Guid? UpdatedBy);
