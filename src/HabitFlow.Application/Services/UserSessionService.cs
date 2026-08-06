using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class SessionSecurityOptions
{
    public int LifetimeDays { get; set; } = 30;
    public int TouchIntervalMinutes { get; set; } = 5;
}

public sealed record AccountSession(Guid Id, string Device, string Browser, string MaskedIp, DateTime CreatedAt, DateTime LastActivityAt, bool IsCurrent);

public sealed class UserSessionService(IUserSessionRepository sessions, TimeProvider clock, Microsoft.Extensions.Options.IOptions<SessionSecurityOptions> options)
{
    public async Task<Guid> StartAsync(Guid userId, Guid? clientId, string? userAgent, string? ipAddress, TimeSpan lifetime, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var session = new UserSession(Guid.NewGuid(), userId, clientId, Limit(userAgent, 500), Limit(ipAddress, 64), now, now, now.Add(lifetime), null, null);
        await sessions.CreateAsync(session, ct);
        return session.Id;
    }

    public async Task<IReadOnlyList<AccountSession>> ListAsync(Guid userId, Guid? clientId, Guid? currentId, CancellationToken ct = default) =>
        (await sessions.ListActiveAsync(userId, clientId, ct)).Select(x => new AccountSession(x.Id, Device(x.UserAgent), Browser(x.UserAgent), MaskIp(x.IpAddress), x.CreatedAt, x.LastActivityAt, x.Id == currentId)).ToList();

    public Task TouchAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default) => sessions.TouchAsync(id, userId, clientId, clock.GetUtcNow().UtcDateTime, TimeSpan.FromMinutes(Math.Clamp(options.Value.TouchIntervalMinutes, 1, 60)), ct);

    private static string Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? "Não informado" : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string Device(string ua) => ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ? "Dispositivo móvel" : "Computador";
    private static string Browser(string ua) => ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Microsoft Edge" : ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox" : ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Chrome" : ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari" : "Navegador desconhecido";
    internal static string MaskIp(string ip)
    {
        if (System.Net.IPAddress.TryParse(ip, out var parsed))
        {
            var bytes = parsed.GetAddressBytes();
            if (bytes.Length == 4) return $"{bytes[0]}.{bytes[1]}.***.***";
            return $"{Convert.ToHexString(bytes.AsSpan(0, 4)).ToLowerInvariant()}:****:****:****";
        }
        return "Não informado";
    }
}

public sealed class SessionRevocationService(IUserSessionRepository sessions, IUserRepository users, IPasswordHasher passwords, AuditService audit)
{
    public async Task<Result> RevokeAsync(Guid userId, Guid? clientId, Guid sessionId, string password, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !passwords.Verify(password, user.PasswordHash)) return Result.Failure("security.current_password_invalid", "A senha atual não confere.");
        if (await sessions.GetOwnedAsync(sessionId, userId, clientId, ct) is null) return Result.Failure("security.session_not_found", "Sessão não encontrada.");
        await sessions.RevokeAsync(sessionId, userId, "user_request", ct);
        await audit.LogAsync("session_revoked", "Sessão encerrada pelo titular.", AuditSeverity.Warning, userId, user.Email, new { sessionId }, ct);
        return Result.Success();
    }

    public async Task<Result> RevokeAllAsync(Guid userId, string password, Guid? exceptSessionId = null, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !passwords.Verify(password, user.PasswordHash)) return Result.Failure("security.current_password_invalid", "A senha atual não confere.");
        await sessions.RevokeAllAsync(userId, exceptSessionId, "user_revoked_all", ct);
        if (exceptSessionId is null) await users.IncrementSessionVersionAsync(userId, ct);
        await audit.LogAsync("all_sessions_revoked", "Todas as sessões foram encerradas pelo titular.", AuditSeverity.Warning, userId, user.Email, ct: ct);
        return Result.Success();
    }
}
