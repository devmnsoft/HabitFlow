namespace HabitFlow.Domain;

public sealed record ApiKeyRecord(Guid Id, Guid ClientId, Guid UserId, string Name, string KeyPrefix,
    string KeyHash, string[] Scopes, DateTime CreatedAt, DateTime? LastUsedAt, DateTime? RevokedAt);

public sealed record IntegrationWebhook(Guid Id, Guid ClientId, Guid UserId, string Name, string Url,
    string[] Events, string SecretCiphertext, bool Enabled, DateTime CreatedAt, DateTime? LastSuccessAt);

public sealed record CalendarFeed(Guid Id, Guid ClientId, Guid UserId, string TokenHash, bool Enabled,
    bool IncludeHabits, bool IncludeRoutines, DateTime CreatedAt, DateTime? LastUsedAt);

public static class IntegrationScopes
{
    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    { "habits.read", "habits.write", "goals.read", "goals.write", "routines.read", "checkins.write", "notifications.read", "profile.read" };
}
