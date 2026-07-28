using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HabitFlow.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Application;

public sealed class EmailOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "GmailSmtp";
    public string FromName { get; set; } = "HabitFlow";
    public string FromAddress { get; set; } = "comercial@mnsoft.com.br";
    public string ReplyToAddress { get; set; } = "comercial@mnsoft.com.br";
    public SmtpOptions Smtp { get; set; } = new();
    public PasswordResetOptions PasswordReset { get; set; } = new();
    public sealed class SmtpOptions { public string Host { get; set; } = "smtp.gmail.com"; public int Port { get; set; } = 587; public bool UseStartTls { get; set; } = true; public string Username { get; set; } = "comercial@mnsoft.com.br"; public string Password { get; set; } = ""; public int TimeoutSeconds { get; set; } = 30; }
    public sealed class PasswordResetOptions { public int TokenLifetimeMinutes { get; set; } = 30; public string PublicBaseUrl { get; set; } = ""; public string[] AllowedBaseUrls { get; set; } = []; public int MaxRequestsPerHourPerEmail { get; set; } = 3; public int MaxRequestsPerHourPerIp { get; set; } = 10; }
}

public sealed class PasswordResetTokenService
{
    public (string RawToken, string Hash) Create()
    {
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (raw, Hash(raw));
    }
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    public PasswordResetTokenValidation Validate(PasswordResetToken? token, DateTime now) => new(token is { UsedAt: null, RevokedAt: null } && token.ExpiresAt > now, token?.UserId);
}

public sealed class PasswordPolicy(IPasswordHasher hasher) : IPasswordPolicy
{
    public string? Validate(string password, User user)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128) return "A senha deve ter entre 8 e 128 caracteres.";
        if (password != password.Trim()) return "A senha não pode começar ou terminar com espaços.";
        if (password.Equals(user.Email, StringComparison.OrdinalIgnoreCase) || password.Equals(user.Name, StringComparison.OrdinalIgnoreCase)) return "Escolha uma senha diferente dos seus dados pessoais.";
        if (hasher.Verify(password, user.PasswordHash)) return "A nova senha deve ser diferente da senha atual.";
        return null;
    }
}

public sealed class PasswordRecoveryService(IUserRepository users, IPasswordResetTokenRepository tokens,
    IPasswordResetRequestRepository requests, ITransactionalEmailOutboxRepository outbox,
    PasswordResetTokenService tokenService, IOptions<EmailOptions> options, TimeProvider clock,
    ILogger<PasswordRecoveryService> logger)
{
    public const string GenericMessage = "Se esse e-mail estiver cadastrado, você receberá uma mensagem com os próximos passos. Verifique também as pastas de spam e promoções.";

    public async Task<string> RequestAsync(PasswordResetRequest request, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var email = NormalizeEmail(request.Email);
        var emailHash = HashPrivate(email);
        var since = now.AddHours(-1);
        var limited = await requests.CountByEmailHashAsync(emailHash, since, ct) >= options.Value.PasswordReset.MaxRequestsPerHourPerEmail
            || await requests.CountByIpHashAsync(request.IpHash, since, ct) >= options.Value.PasswordReset.MaxRequestsPerHourPerIp;
        await requests.AddAsync(emailHash, request.IpHash, now, ct);
        var user = IsValidEmail(email) ? await users.GetByEmailAsync(email, ct) : null;
        if (!limited && user is { AccountStatus: AccountStatus.Active })
        {
            var (raw, hash) = tokenService.Create();
            await tokens.RevokeActiveAsync(user.Id, now, ct);
            await tokens.CreateAsync(new(Guid.NewGuid(), user.Id, hash, now.AddMinutes(options.Value.PasswordReset.TokenLifetimeMinutes), null, null, now), request.IpHash, request.UserAgentHash, request.CorrelationId, ct);
            var link = BuildPublicLink(raw);
            var content = TransactionalEmailService.PasswordReset(user.Name, link, options.Value.PasswordReset.TokenLifetimeMinutes);
            await outbox.EnqueueAsync(new(Guid.NewGuid(), user.ClientId, user.Id, "PasswordReset", user.Email, content.Subject,
                JsonSerializer.Serialize(content), TransactionalEmailStatus.Pending, $"password-reset:{hash}", 0, now, now), ct);
        }
        logger.LogInformation("Password recovery request processed. Limited={Limited}", limited);
        return GenericMessage;
    }

    private string BuildPublicLink(string token)
    {
        var configured = options.Value.PasswordReset.PublicBaseUrl.TrimEnd('/');
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri)) throw new InvalidOperationException("Email:PasswordReset:PublicBaseUrl inválida.");
        var allowed = options.Value.PasswordReset.AllowedBaseUrls;
        if (allowed.Length > 0 && !allowed.Any(x => string.Equals(x.TrimEnd('/'), configured, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("PublicBaseUrl não está na allowlist.");
        return $"{uri.GetLeftPart(UriPartial.Authority)}/reset-password?token={Uri.EscapeDataString(token)}";
    }
    public static string NormalizeEmail(string value) => (value ?? "").Trim().ToLowerInvariant();
    private static bool IsValidEmail(string value) { if (value.Length is 0 or > 254) return false; try { return new MailAddress(value).Address == value; } catch (FormatException) { return false; } }
    public static string HashPrivate(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class PasswordResetService(IPasswordResetTokenRepository tokens, IUserRepository users,
    IPasswordPolicy policy, IPasswordHasher hasher, IUserSessionRevocationService sessions,
    ITransactionalEmailOutboxRepository outbox, IUnitOfWork unitOfWork, PasswordResetTokenService tokenService,
    TimeProvider clock)
{
    public async Task<PasswordResetTokenValidation> ValidateAsync(string token, CancellationToken ct = default) =>
        tokenService.Validate(string.IsNullOrWhiteSpace(token) ? null : await tokens.GetForUpdateAsync(PasswordResetTokenService.Hash(token), ct), clock.GetUtcNow().UtcDateTime);

    public async Task<PasswordResetResult> ResetAsync(string rawToken, string newPassword, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var token = string.IsNullOrWhiteSpace(rawToken) ? null : await tokens.GetForUpdateAsync(PasswordResetTokenService.Hash(rawToken), ct);
            if (!tokenService.Validate(token, now).IsValid) { await unitOfWork.RollbackAsync(ct); return new(false, "Este link não está mais disponível. Solicite uma nova recuperação de senha."); }
            var user = await users.GetByIdAsync(token!.UserId, ct);
            if (user is null || user.AccountStatus != AccountStatus.Active) { await unitOfWork.RollbackAsync(ct); return new(false, "Este link não está mais disponível. Solicite uma nova recuperação de senha."); }
            var error = policy.Validate(newPassword, user);
            if (error is not null) { await unitOfWork.RollbackAsync(ct); return new(false, error); }
            await users.UpdatePasswordAndSessionVersionAsync(user.Id, hasher.Hash(newPassword), ct);
            await tokens.MarkUsedAndRevokeOthersAsync(token.Id, user.Id, now, ct);
            await sessions.RevokeAsync(user.Id, ct);
            var content = TransactionalEmailService.PasswordChanged(user.Name);
            await outbox.EnqueueAsync(new(Guid.NewGuid(), user.ClientId, user.Id, "PasswordChanged", user.Email, content.Subject, JsonSerializer.Serialize(content), TransactionalEmailStatus.Pending, $"password-changed:{token.Id}", 0, now, now), ct);
            await unitOfWork.CommitAsync(ct);
            return new(true);
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }
}

public sealed record TransactionalEmailContent(string Subject, string Text, string Html);
public static class TransactionalEmailService
{
    public static TransactionalEmailContent PasswordReset(string name, string link, int minutes)
    {
        var first = HtmlEncoder.Default.Encode(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Olá");
        var safeLink = HtmlEncoder.Default.Encode(link);
        var text = $"Olá, {first}.\n\nRecebemos uma solicitação para criar uma nova senha para seu acesso ao HabitFlow.\n\nUse este link: {link}\nEle ficará disponível por {minutes} minutos e poderá ser usado apenas uma vez.\n\nVocê não solicitou essa alteração? Ignore esta mensagem. Sua senha atual continuará funcionando.\n\nEsta é uma mensagem automática do HabitFlow, desenvolvido pela MNSOFT.";
        var html = $"<h1>Olá, {first}.</h1><p>Recebemos uma solicitação para criar uma nova senha para seu acesso ao HabitFlow.</p><p>Este link ficará disponível por {minutes} minutos e poderá ser usado apenas uma vez.</p><p><a href=\"{safeLink}\">CRIAR NOVA SENHA</a></p><p>Você não solicitou essa alteração? Ignore esta mensagem. Sua senha atual continuará funcionando.</p><footer>Esta é uma mensagem automática do HabitFlow, desenvolvido pela MNSOFT.</footer>";
        return new("Crie uma nova senha para o HabitFlow", text, html);
    }
    public static TransactionalEmailContent PasswordChanged(string name)
    {
        var first = HtmlEncoder.Default.Encode(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Olá");
        return new("Sua senha do HabitFlow foi alterada", $"Olá, {first}.\n\nSua senha foi alterada com sucesso.\n\nSe foi você, não é necessário fazer mais nada.\n\nCaso não reconheça essa alteração, entre em contato imediatamente com o suporte pelo e-mail comercial@mnsoft.com.br.", $"<h1>Olá, {first}.</h1><p>Sua senha foi alterada com sucesso.</p><p>Se foi você, não é necessário fazer mais nada.</p><p>Caso não reconheça essa alteração, entre em contato imediatamente com o suporte pelo e-mail comercial@mnsoft.com.br.</p>");
    }
}
