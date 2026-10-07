namespace HabitFlow.Application;

public sealed record LoginDto(string Email, string Password, string? ReturnUrl = null);
