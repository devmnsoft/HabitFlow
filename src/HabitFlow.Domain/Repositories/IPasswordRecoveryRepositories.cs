namespace HabitFlow.Domain;

public interface IPasswordResetTokenRepository
{
    Task RevokeActiveAsync(Guid userId, DateTime now, CancellationToken ct = default);
    Task CreateAsync(PasswordResetToken token, string? ipHash, string? userAgentHash, string correlationId, CancellationToken ct = default);
    Task<PasswordResetToken?> GetForUpdateAsync(string tokenHash, CancellationToken ct = default);
    Task MarkUsedAndRevokeOthersAsync(Guid tokenId, Guid userId, DateTime now, CancellationToken ct = default);
}

public interface IPasswordResetRequestRepository
{
    Task<int> CountByEmailHashAsync(string emailHash, DateTime since, CancellationToken ct = default);
    Task<int> CountByIpHashAsync(string ipHash, DateTime since, CancellationToken ct = default);
    Task AddAsync(string emailHash, string ipHash, DateTime createdAt, CancellationToken ct = default);
}

public interface ITransactionalEmailOutboxRepository
{
    Task EnqueueAsync(TransactionalEmailMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionalEmailMessage>> ClaimBatchAsync(int size, CancellationToken ct = default);
    Task MarkSentAsync(Guid id, DateTime sentAt, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string sanitizedError, DateTime nextAttempt, int maxAttempts, CancellationToken ct = default);
}

public interface ITransactionalEmailSender { Task SendAsync(TransactionalEmailMessage message, CancellationToken ct = default); }
public interface IPasswordPolicy { string? Validate(string password, User user); }
public interface IUserSessionRevocationService { Task RevokeAsync(Guid userId, CancellationToken ct = default); }
