using System.Security.Cryptography;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record CreatedSecret<T>(T Value, string Secret);

public sealed class IntegrationService(IIntegrationRepository repository)
{
    public static string HashSecret(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public async Task<CreatedSecret<ApiKeyRecord>> CreateKeyAsync(Guid clientId, Guid userId, string name, IEnumerable<string> scopes, CancellationToken ct = default)
    {
        name = name.Trim();
        if (name.Length is < 2 or > 80) throw new ArgumentException("O nome deve ter entre 2 e 80 caracteres.");
        var normalized = scopes.Distinct(StringComparer.Ordinal).ToArray();
        if (normalized.Length == 0 || normalized.Any(x => !IntegrationScopes.Allowed.Contains(x))) throw new ArgumentException("Escopos inválidos.");
        var secret = "hf_live_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var key = new ApiKeyRecord(Guid.NewGuid(), clientId, userId, name, secret[..15], HashSecret(secret), normalized, DateTime.UtcNow, null, null);
        await repository.CreateApiKeyAsync(key, ct);
        await repository.AddAuditAsync(clientId, userId, "api_key.created", new { key.Id, key.Name, key.Scopes }, ct);
        return new(key, secret);
    }

    public async Task<CreatedSecret<CalendarFeed>> RotateCalendarAsync(Guid clientId, Guid userId, bool enabled, bool habits, bool routines, CancellationToken ct = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var current = await repository.GetCalendarFeedAsync(clientId, userId, ct);
        var feed = new CalendarFeed(current?.Id ?? Guid.NewGuid(), clientId, userId, HashSecret(token), enabled, habits, routines, current?.CreatedAt ?? DateTime.UtcNow, current?.LastUsedAt);
        await repository.UpsertCalendarFeedAsync(feed, ct);
        await repository.AddAuditAsync(clientId, userId, current is null ? "calendar_feed.enabled" : "calendar_feed.token_rotated", new { feed.Id, enabled, habits, routines }, ct);
        return new(feed, token);
    }
}
