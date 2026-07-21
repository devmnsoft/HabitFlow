namespace HabitFlow.Application;

public sealed record RegisterDto(string Name, string Email, string Password, string ConfirmPassword);
