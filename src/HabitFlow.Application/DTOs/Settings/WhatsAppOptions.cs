namespace HabitFlow.Application;

public sealed record WhatsAppOptions(bool Enabled, string? Number, string? DefaultMessage, string? ButtonText);
