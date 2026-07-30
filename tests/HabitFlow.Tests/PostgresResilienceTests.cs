using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public class PostgresResilienceTests
{
    [Fact]
    public void PostgresErrorHelper_identifica_banco_ausente_por_sqlstate_3d000()
    {
        var ex = new InvalidOperationException("database missing");
        ex.Data["SqlState"] = PostgresErrorHelper.DatabaseMissingSqlState;

        Assert.True(PostgresErrorHelper.IsDatabaseMissing(ex));
        Assert.True(PostgresErrorHelper.IsConnectionFailure(ex));
        Assert.Equal(PostgresErrorHelper.FriendlyDatabaseMissingMessage, PostgresErrorHelper.BuildFriendlyMessage(ex));
    }


    [Fact]
    public void PostgresErrorHelper_mapeia_28p01_para_codigo_amigavel()
    {
        var ex = new InvalidOperationException("28P01: password authentication failed");
        ex.Data["SqlState"] = PostgresErrorHelper.InvalidPasswordSqlState;

        Assert.True(PostgresErrorHelper.IsInvalidPassword(ex));
        Assert.Equal(PostgresErrorHelper.InvalidPasswordCode, PostgresErrorHelper.ToFriendlyCode(ex));
        Assert.Equal(PostgresErrorHelper.FriendlyInvalidPasswordMessage, PostgresErrorHelper.ToFriendlyMessage(ex));
    }

    [Fact]
    public async Task AuthService_retorna_mensagem_amigavel_e_nao_aciona_auditoria_quando_banco_nao_existe()
    {
        var users = new MissingDatabaseUsers();
        var auditRepo = new CountingAuditRepository();
        var audit = new AuditService(auditRepo, new LogSanitizer(), NullLogger<AuditService>.Instance);
        var service = new AuthService(users, new BCryptPasswordHasher(), audit, NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginDto("admin@habitflow.local", "Admin@123"), "127.0.0.1", "test");

        Assert.False(result.IsSuccess);
        Assert.Equal(PostgresErrorHelper.DatabaseMissingCode, result.Error.Code);
        Assert.Equal("O banco de dados configurado não foi encontrado.", result.Error.Message);
        Assert.Equal(0, auditRepo.Count);
    }

    [Fact]
    public async Task AuditService_nao_derruba_fluxo_quando_banco_nao_existe()
    {
        var audit = new AuditService(new MissingDatabaseAuditRepository(), new LogSanitizer(), NullLogger<AuditService>.Instance);
        await audit.LogAsync("login_error", "Erro controlado", AuditSeverity.Error);
    }

    [Fact]
    public async Task DatabaseDiagnosticsService_retorna_unhealthy_com_erro_amigavel()
    {
        var service = new DatabaseDiagnosticsService(new MissingDatabaseDiagnosticsRepository(), NullLogger<DatabaseDiagnosticsService>.Instance);
        var result = await service.GetAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal("unhealthy", result.Value?.Status);
        Assert.Equal(PostgresErrorHelper.FriendlyDatabaseMissingMessage, result.Value?.ErrorMessage);
    }

    private static Exception MissingDatabaseException()
    {
        var ex = new InvalidOperationException("3D000: database does not exist");
        ex.Data["SqlState"] = PostgresErrorHelper.DatabaseMissingSqlState;
        return ex;
    }

    private sealed class MissingDatabaseUsers : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task CreateAsync(User user, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task UpdatePasswordAndSessionVersionAsync(
            Guid userId,
            string passwordHash,
            CancellationToken ct = default)
            => throw MissingDatabaseException();
        public Task AddLoginAttemptAsync(LoginAttempt attempt, CancellationToken ct = default) => throw MissingDatabaseException();
    }

    private sealed class MissingDatabaseAuditRepository : IAuditRepository
    {
        public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) => throw MissingDatabaseException();
        public Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SystemAuditLog>>(Array.Empty<SystemAuditLog>());
    }

    private sealed class CountingAuditRepository : IAuditRepository
    {
        public int Count { get; private set; }
        public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) { Count++; return Task.CompletedTask; }
        public Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SystemAuditLog>>(Array.Empty<SystemAuditLog>());
    }

    private sealed class MissingDatabaseDiagnosticsRepository : IDatabaseDiagnosticsRepository
    {
        public Task<DatabaseDiagnostics> GetAsync(CancellationToken ct = default) => throw MissingDatabaseException();
    }
}
