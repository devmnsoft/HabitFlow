namespace HabitFlow.Application;

public sealed record HabitDto(Guid Id, string Name, string Color, string? Category, bool DoneToday, bool IsArchived);
