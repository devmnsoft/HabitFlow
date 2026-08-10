namespace HabitFlow.Domain;

public sealed record UserPrivacyConsent(Guid UserId, string ConsentKey, bool Granted, DateTime UpdatedAt);
