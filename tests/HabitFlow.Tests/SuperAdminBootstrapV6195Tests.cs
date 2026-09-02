using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SuperAdminBootstrapV6195Tests
{
    [Theory]
    [InlineData("18.160.057/0001-13", "18160057000113")]
    [InlineData("18160057000113", "18160057000113")]
    [InlineData(" COMERCIAL@MNSOFT.COM.BR ", "comercial@mnsoft.com.br")]
    public void Login_normalizes_email_and_document(string value, string expected) =>
        Assert.Equal(expected, AuthService.NormalizeLogin(value));

    [Fact]
    public void Migration_never_contains_an_initial_password()
    {
        var sql = File.ReadAllText(RepositoryRootLocator.PathTo("database/migrations/086_v6195_superadmin_bootstrap.sql"));
        Assert.DoesNotContain("password_hash", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on conflict", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Startup_requires_secret_and_keeps_global_policy_server_side()
    {
        var hosted = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Services/SuperAdminBootstrapHostedService.cs"));
        var controller = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Controllers/SuperAdminController.cs"));
        Assert.Contains("HABITFLOW_SUPERADMIN_INITIAL_PASSWORD", hosted);
        Assert.Contains("environment.IsDevelopment()", hosted);
        Assert.Contains("MNSoft@2026!TrocarAgora", hosted);
        Assert.Contains("LogError", hosted);
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", controller);
    }

    [Fact]
    public async Task Bootstrap_creates_active_global_superadmin_with_official_hash_and_required_change()
    {
        var repository = new BootstrapRepository();
        var service = new SuperAdminBootstrapService(repository, new BCryptPasswordHasher());

        var result = await service.BootstrapAsync(Options(), "test");

        Assert.True(result.Created);
        Assert.True(result.PasswordHashUpdated);
        Assert.Equal(UserRole.SuperAdmin, result.User.Role);
        Assert.Equal(AccountStatus.Active, result.User.AccountStatus);
        Assert.Null(result.User.ClientId);
        Assert.True(result.User.MustChangePassword);
        Assert.True(new BCryptPasswordHasher().Verify("MNSoft@2026!TrocarAgora", result.User.PasswordHash));
    }

    [Fact]
    public async Task Bootstrap_is_idempotent_and_repairs_role_without_replacing_valid_hash()
    {
        var repository = new BootstrapRepository();
        var service = new SuperAdminBootstrapService(repository, new BCryptPasswordHasher());
        await service.BootstrapAsync(Options(), "first");
        repository.User = repository.User! with { Role = UserRole.User, AccountStatus = AccountStatus.Suspended, ClientId = Guid.NewGuid() };

        var result = await service.BootstrapAsync(Options(), "second");

        Assert.False(result.Created);
        Assert.False(result.PasswordHashUpdated);
        Assert.Single(repository.Ids);
        Assert.Equal(UserRole.SuperAdmin, result.User.Role);
        Assert.Equal(AccountStatus.Active, result.User.AccountStatus);
        Assert.Null(result.User.ClientId);
    }

    [Fact]
    public async Task Bootstrap_repairs_invalid_hash_and_requires_password_change()
    {
        var repository = new BootstrapRepository { User = NewUser("invalid-hash", false) };
        var result = await new SuperAdminBootstrapService(repository, new BCryptPasswordHasher()).BootstrapAsync(Options(), "repair");

        Assert.True(result.PasswordHashUpdated);
        Assert.True(result.User.MustChangePassword);
        Assert.True(new BCryptPasswordHasher().Verify("MNSoft@2026!TrocarAgora", result.User.PasswordHash));
    }

    private static SuperAdminOptions Options() => new()
    {
        Email = " COMERCIAL@MNSOFT.COM.BR ",
        Document = "18.160.057/0001-13",
        InitialPassword = "MNSoft@2026!TrocarAgora"
    };

    private static User NewUser(string hash, bool mustChange) => new(Guid.NewGuid(), "MNSOFT", "comercial@mnsoft.com.br", hash, null,
        UserRole.SuperAdmin, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, true,
        null, null, null, null, DateTime.UtcNow, DateTime.UtcNow, null, 1, mustChange);

    private sealed class BootstrapRepository : ISuperAdminProvisioningRepository
    {
        public User? User { get; set; }
        public HashSet<Guid> Ids { get; } = [];
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(User);
        public Task<(User User, bool Created, bool Updated, bool PasswordHashUpdated)> BootstrapAsync(string name, string email, string document, string? passwordHash, string correlationId, CancellationToken ct)
        {
            var created = User is null;
            var id = User?.Id ?? Guid.NewGuid();
            User = NewUser(passwordHash ?? User!.PasswordHash, passwordHash is not null) with { Id = id, Name = name, Email = email };
            Ids.Add(id);
            return Task.FromResult((User, created, !created, passwordHash is not null));
        }
        public Task<User> CreateOrPromoteAsync(string name, string email, string passwordHash, bool mustChangePassword, string actor, string reason, string correlationId, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> PromoteAsync(string email, string actor, string reason, string correlationId, CancellationToken ct) => throw new NotSupportedException();
        public Task ResetPasswordAsync(Guid userId, string passwordHash, string actor, string reason, string correlationId, CancellationToken ct) => throw new NotSupportedException();
    }
}
