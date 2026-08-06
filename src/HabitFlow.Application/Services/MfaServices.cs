using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using HabitFlow.Domain;
using Microsoft.AspNetCore.DataProtection;

namespace HabitFlow.Application;

public sealed class TotpSecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("HabitFlow.Mfa.TotpSecret.v1");
    public string Protect(byte[] secret) => _protector.Protect(Convert.ToBase64String(secret));
    public byte[] Unprotect(string value) => Convert.FromBase64String(_protector.Unprotect(value));
}

public sealed record TotpEnrollment(string ManualKey, string OtpAuthUri);

public sealed class TotpEnrollmentService(TotpSecretProtector protector, IUserMfaRepository repository, TimeProvider clock)
{
    public async Task<TotpEnrollment> StartAsync(Guid userId, Guid? clientId, string email, CancellationToken ct = default)
    {
        var secret = RandomNumberGenerator.GetBytes(20);
        await repository.SavePendingAsync(userId, clientId, protector.Protect(secret), clock.GetUtcNow().UtcDateTime, ct);
        var key = Base32.Encode(secret);
        return new(key, $"otpauth://totp/HabitFlow:{Uri.EscapeDataString(email)}?secret={key}&issuer=HabitFlow&algorithm=SHA1&digits=6&period=30");
    }
}

public sealed class TotpValidationService(TimeProvider clock)
{
    public bool TryValidate(byte[] secret, string code, out long timeStep)
    {
        timeStep = 0;
        if (code.Length != 6 || !code.All(char.IsAsciiDigit)) return false;
        var current = clock.GetUtcNow().ToUnixTimeSeconds() / 30;
        for (var candidate = current - 1; candidate <= current + 1; candidate++)
        {
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Generate(secret, candidate)), Encoding.ASCII.GetBytes(code)))
            { timeStep = candidate; return true; }
        }
        return false;
    }

    private static string Generate(byte[] secret, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(secret, counter);
        var offset = hash[^1] & 0xf;
        var value = (BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7fffffff) % 1_000_000;
        return value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
}

public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0; var bits = 0;
        foreach (var value in data) { buffer = (buffer << 8) | value; bits += 8; while (bits >= 5) { output.Append(Alphabet[(buffer >> (bits -= 5)) & 31]); } }
        if (bits > 0) output.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }
}

public sealed class RecoveryCodeService(IUserMfaRepository repository, TimeProvider clock)
{
    public async Task<IReadOnlyList<string>> RegenerateAsync(Guid userId, Guid? clientId, CancellationToken ct = default)
    {
        var codes = Enumerable.Range(0, 8).Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(5))).ToArray();
        await repository.ReplaceRecoveryCodesAsync(userId, clientId, codes.Select(Hash).ToArray(), clock.GetUtcNow().UtcDateTime, ct);
        return codes;
    }
    public Task<bool> ConsumeAsync(Guid userId, Guid? clientId, string code, CancellationToken ct = default) =>
        repository.ConsumeRecoveryCodeAsync(userId, clientId, Hash(code.Trim()), clock.GetUtcNow().UtcDateTime, ct);
    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.ToUpperInvariant())));
}

public sealed class MfaChallengeService(IUserMfaRepository repository, TotpSecretProtector protector, TotpValidationService validator, TimeProvider clock)
{
    public Task<UserMfaChallenge> StartAsync(Guid userId, Guid? clientId, CancellationToken ct = default) =>
        repository.CreateChallengeAsync(userId, clientId, clock.GetUtcNow().AddMinutes(5).UtcDateTime, ct);

    public async Task<bool> ValidateAsync(Guid challengeId, Guid userId, Guid? clientId, string code, CancellationToken ct = default)
    {
        var challenge = await repository.GetChallengeAsync(challengeId, userId, clientId, ct);
        if (challenge is null || challenge.VerifiedAt is not null || challenge.FailedAttempts >= 5 || challenge.ExpiresAt <= clock.GetUtcNow().UtcDateTime) return false;
        var setting = await repository.GetAsync(userId, clientId, ct);
        if (setting is null || !setting.IsEnabled || !validator.TryValidate(protector.Unprotect(setting.ProtectedSecret), code, out var step) ||
            !await repository.AcceptTimeStepAsync(userId, clientId, step, ct))
        { await repository.RegisterChallengeFailureAsync(challengeId, userId, clientId, ct); return false; }
        await repository.VerifyChallengeAsync(challengeId, userId, clientId, clock.GetUtcNow().UtcDateTime, ct);
        await repository.AddSecurityEventAsync(userId, clientId, "MfaChallengeSucceeded", clock.GetUtcNow().UtcDateTime, ct);
        return true;
    }
}

public sealed class SuperAdminMfaRequirementService(IUserMfaRepository repository)
{
    public async Task<bool> IsEnrollmentRequiredAsync(Guid userId, Guid? clientId, CancellationToken ct = default) =>
        (await repository.GetAsync(userId, clientId, ct))?.IsEnabled != true;
}
