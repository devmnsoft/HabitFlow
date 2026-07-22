namespace HabitFlow.Infrastructure;

public static class DbEnum
{
    public static string Text<TEnum>(TEnum value) where TEnum : struct, Enum
        => value.ToString();

    public static string? Text<TEnum>(TEnum? value) where TEnum : struct, Enum
        => value?.ToString();
}
