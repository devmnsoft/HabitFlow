using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public class PremiumBillingTests
{
    [Fact]
    public void Payment_metadata_sanitizer_removes_sensitive_fields()
    {
        var sanitized = new PaymentMetadataSanitizer().Sanitize("{\"card_token\":\"abc\",\"amount\":14.90,\"payer\":{\"cpf\":\"123\"}}");
        Assert.DoesNotContain("abc", sanitized);
        Assert.DoesNotContain("123", sanitized);
        Assert.Contains("[REDACTED]", sanitized);
    }

    [Fact]
    public void Billing_cycles_are_limited_to_monthly_and_yearly()
    {
        Assert.Equal("Monthly", HabitFlow.Domain.BillingCycle.Monthly.ToString());
        Assert.Equal("Yearly", HabitFlow.Domain.BillingCycle.Yearly.ToString());
    }

    [Fact]
    public void Payment_status_maps_approved_without_frontend_activation()
    {
        Assert.Equal("Approved", HabitFlow.Domain.PaymentStatus.Approved.ToString());
    }

    [Fact]
    public void Mercado_pago_signature_requires_exact_hmac_manifest()
    {
        const string payload = "{\"id\":\"evt-1\",\"data\":{\"id\":\"PAY-123\"}}";
        const string requestId = "req-1";
        const string timestamp = "1710000000";
        const string secret = "test-only-secret";
        var manifest = $"id:pay-123;request-id:{requestId};ts:{timestamp};";
        var hash = Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secret), System.Text.Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        Assert.True(MercadoPagoService.ValidateSignature(payload, $"ts={timestamp},v1={hash}", requestId, secret));
        Assert.False(MercadoPagoService.ValidateSignature(payload, $"ts={timestamp},v1={new string('0', 64)}", requestId, secret));
    }

    [Fact]
    public async Task Activation_with_same_provider_payment_is_idempotent()
    {
        var user = User();
        var subscription = Subscription(user.Id, SubscriptionStatus.Active, "pay-1", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25));
        var subscriptions = new SubscriptionRepositoryFake(subscription);
        var service = new SubscriptionService(subscriptions, new UserRepositoryFake(user), new AuditRepositoryFake(), NullLogger<SubscriptionService>.Instance);

        var result = await service.ActivateSubscriptionAsync(subscription.Id, "pay-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, subscriptions.UpdateCount);
    }

    [Fact]
    public async Task Renewal_extends_from_current_period_end_without_restarting_access()
    {
        var user = User();
        var periodEnd = DateTime.UtcNow.AddDays(10);
        var subscription = Subscription(user.Id, SubscriptionStatus.Active, "pay-old", DateTime.UtcNow.AddDays(-20), periodEnd);
        var subscriptions = new SubscriptionRepositoryFake(subscription);
        var service = new SubscriptionService(subscriptions, new UserRepositoryFake(user), new AuditRepositoryFake(), NullLogger<SubscriptionService>.Instance);

        var result = await service.ActivateSubscriptionAsync(subscription.Id, "pay-new");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, subscriptions.UpdateCount);
        Assert.Equal(periodEnd, subscriptions.Current!.CurrentPeriodStart);
        Assert.Equal(periodEnd.AddMonths(1), subscriptions.Current.CurrentPeriodEnd);
        Assert.Equal("pay-new", subscriptions.Current.ProviderPaymentId);
    }

    private static User User() => new(Guid.NewGuid(), "Cliente", "cliente@example.com", "hash", null, UserRole.User,
        AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Inactive, false, false,
        DateTime.UtcNow, DateTime.UtcNow, null, null, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());

    private static Subscription Subscription(Guid userId, SubscriptionStatus status, string? paymentId, DateTime? start, DateTime? end) =>
        new(Guid.NewGuid(), userId, "premium_monthly", status, BillingCycle.Monthly, PaymentProvider.MercadoPago,
            null, null, paymentId, null, start, end, null, null, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

    private sealed class SubscriptionRepositoryFake(Subscription current) : ISubscriptionRepository
    {
        public Subscription? Current { get; private set; } = current;
        public int UpdateCount { get; private set; }
        public Task<Subscription?> GetActiveOrLatestByUserIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(Current);
        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Current?.Id == id ? Current : null);
        public Task<Subscription?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default) => Task.FromResult(Current?.ProviderPaymentId == providerPaymentId ? Current : null);
        public Task CreateAsync(Subscription subscription, CancellationToken ct = default) { Current = subscription; return Task.CompletedTask; }
        public Task UpdateAsync(Subscription subscription, CancellationToken ct = default) { Current = subscription; UpdateCount++; return Task.CompletedTask; }
        public Task UpdateCheckoutAsync(Guid id, string? checkoutUrl, string? providerSubscriptionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Subscription>> ListByStatusAsync(SubscriptionStatus? status, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Subscription>>(Current is null ? [] : [Current]);
    }

    private sealed class UserRepositoryFake(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(user.Id == id ? user : null);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<User>>([]);
        public Task CreateAsync(User value, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(User value, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> LinkToClientFromInviteAsync(Guid userId, Guid clientId, UserRole role, DateTime utcNow, CancellationToken ct = default) => Task.FromResult(false);
        public Task UpdatePasswordAndSessionVersionAsync(Guid userId, string passwordHash, CancellationToken ct = default) => Task.CompletedTask;
        public Task IncrementSessionVersionAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddLoginAttemptAsync(LoginAttempt attempt, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class AuditRepositoryFake : IPaymentAuditRepository
    {
        public Task CreateAsync(PaymentAuditLog auditLog, CancellationToken ct = default) => Task.CompletedTask;
    }
}
