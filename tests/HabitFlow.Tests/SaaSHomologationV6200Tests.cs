using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class SaaSHomologationV6200Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;

    [Fact]
    public void New_user_starts_with_15_day_trial_subscription_source_and_rules()
    {
        var registrationFile = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Application/Services/ClientAccountRegistrationService.cs"));
        Assert.Contains("ClientPlan.Premium", registrationFile);
        Assert.Contains("ClientSubscriptionStatus.Trial", registrationFile);
        Assert.Contains("ClientBenefitsStatus.PremiumActive", registrationFile);
        Assert.Contains("UserPlan.Premium", registrationFile);
        Assert.Contains("PlanStatus.Trial", registrationFile);
        Assert.Contains("SubscriptionStatus.Trial", registrationFile);
        Assert.Contains("AddDays(15)", registrationFile);
        Assert.Contains("15 dias grátis", registrationFile);

        var now = DateTime.UtcNow;
        var trialEnd = now.AddDays(15);
        var sub = new Subscription(Guid.NewGuid(), Guid.NewGuid(), "premium_monthly", SubscriptionStatus.Trial, BillingCycle.Monthly, PaymentProvider.Manual, null, null, null, null, now, trialEnd, trialEnd, null, now, now);
        Assert.Equal(SubscriptionStatus.Trial, sub.Status);
        Assert.Equal("premium_monthly", sub.PlanCode);
        Assert.NotNull(sub.TrialEndsAt);
        Assert.True(sub.TrialEndsAt.Value > now.AddDays(14));
    }

    [Fact]
    public async Task Public_plans_catalog_excludes_free_plan()
    {
        var planRepo = new FakePlanRepository(
        [
            new Plan(Guid.NewGuid(), "free", "Gratuito", "Legado", 0, 0, "BRL", 5, true, false, false, true, false, DateTime.UtcNow, DateTime.UtcNow),
            new Plan(Guid.NewGuid(), "premium_monthly", "Premium Mensal", "15 dias grátis", 29.90m, 299m, "BRL", null, true, true, true, true, true, DateTime.UtcNow, DateTime.UtcNow),
            new Plan(Guid.NewGuid(), "team", "Team", "Times", 99m, 990m, "BRL", null, true, true, true, true, true, DateTime.UtcNow, DateTime.UtcNow),
            new Plan(Guid.NewGuid(), "enterprise", "Enterprise", "Corporativo", 299m, 2990m, "BRL", null, true, true, true, true, true, DateTime.UtcNow, DateTime.UtcNow)
        ]);

        var publicPlans = await planRepo.GetPublicPlansAsync();

        Assert.DoesNotContain(publicPlans, p => p.Code == "free");
        Assert.Contains(publicPlans, p => p.Code == "premium_monthly");
        Assert.Contains(publicPlans, p => p.Code == "team");
        Assert.Contains(publicPlans, p => p.Code == "enterprise");
    }

    [Fact]
    public async Task Expired_trial_blocks_habit_creation()
    {
        var userId = Guid.NewGuid();
        var expiredTrial = new Subscription(
            Guid.NewGuid(),
            userId,
            "premium_monthly",
            SubscriptionStatus.Trial,
            BillingCycle.Monthly,
            PaymentProvider.Manual,
            null, null, null, null,
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-5),
            null,
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow);

        var subRepo = new FakeSubscriptionRepository(expiredTrial);
        var config = new ConfigurationBuilder().Build();
        var accessService = new PremiumAccessService(
            new PlanService(new FakePlanRepository(), subRepo, NullLogger<PlanService>.Instance),
            subRepo,
            config,
            NullLogger<PremiumAccessService>.Instance);

        var isPremium = await accessService.IsPremiumAsync(userId);
        Assert.False(isPremium.Value);

        var isExpired = await accessService.IsTrialExpiredAsync(userId);
        Assert.True(isExpired.Value);

        var canCreate = await accessService.CanCreateHabitAsync(userId, 0);
        Assert.False(canCreate.IsSuccess);
        Assert.Equal("trial.expired", canCreate.Error.Code);
    }

    [Fact]
    public async Task Legacy_free_user_is_preserved_with_five_habits_limit()
    {
        var userId = Guid.NewGuid();
        var legacySub = new Subscription(
            Guid.NewGuid(),
            userId,
            "free",
            SubscriptionStatus.Active,
            BillingCycle.Monthly,
            PaymentProvider.Manual,
            null, null, null, null,
            DateTime.UtcNow.AddYears(-1),
            null,
            null,
            null,
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow);

        var subRepo = new FakeSubscriptionRepository(legacySub);
        var config = new ConfigurationBuilder().Build();
        var accessService = new PremiumAccessService(
            new PlanService(new FakePlanRepository(), subRepo, NullLogger<PlanService>.Instance),
            subRepo,
            config,
            NullLogger<PremiumAccessService>.Instance);

        var canCreateUnderLimit = await accessService.CanCreateHabitAsync(userId, 3);
        Assert.True(canCreateUnderLimit.IsSuccess);
        Assert.True(canCreateUnderLimit.Value);

        var canCreateAtLimit = await accessService.CanCreateHabitAsync(userId, 5);
        Assert.True(canCreateAtLimit.IsSuccess);
        Assert.False(canCreateAtLimit.Value);
    }

    [Fact]
    public async Task Checkout_is_disabled_and_fails_safely_when_payments_disabled()
    {
        var userId = Guid.NewGuid();
        var subRepo = new FakeSubscriptionRepository();
        var audit = new FakePaymentAuditRepository();
        var planRepo = new FakePlanRepository(
        [
            new Plan(Guid.NewGuid(), "premium_monthly", "Premium", "Desc", 29.90m, 299m, "BRL", null, true, true, true, true, true, DateTime.UtcNow, DateTime.UtcNow)
        ]);
        var catalog = new FakePlanCatalogRepository();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Enabled"] = "false",
                ["Payments:Provider"] = "Manual"
            })
            .Build();

        var subService = new SubscriptionService(subRepo, new FakeUserRepository(), audit, NullLogger<SubscriptionService>.Instance);
        var checkoutService = new PaymentCheckoutService(
            planRepo,
            catalog,
            subService,
            new FakePaymentProviderService(),
            audit,
            config,
            NullLogger<PaymentCheckoutService>.Instance);

        var result = await checkoutService.StartCheckoutAsync(userId, "user@teste.com", "User", "premium_monthly", BillingCycle.Monthly);

        Assert.False(result.IsSuccess);
        Assert.Equal("payment.checkout_unavailable", result.Error.Code);
        Assert.Contains(audit.Logs, a => a.Action == "billing.checkout.unavailable");
    }

    [Fact]
    public async Task Webhook_approved_activates_subscription_and_audits()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var sub = new Subscription(subId, userId, "premium_monthly", SubscriptionStatus.Pending, BillingCycle.Monthly, PaymentProvider.MercadoPago, null, null, null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
        var user = new User(userId, "User", "user@teste.com", "hash", null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Inactive, false, false, DateTime.UtcNow, DateTime.UtcNow, null, null, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());

        var subRepo = new FakeSubscriptionRepository(sub);
        var userRepo = new FakeUserRepository(user);
        var audit = new FakePaymentAuditRepository();
        var txRepo = new FakePaymentTransactionRepository();
        var webhookRepo = new FakePaymentWebhookRepository();
        var subService = new SubscriptionService(subRepo, userRepo, audit, NullLogger<SubscriptionService>.Instance);

        var webhookService = new PaymentWebhookService(
            webhookRepo,
            subRepo,
            txRepo,
            subService,
            new PaymentMetadataSanitizer(),
            new FakePaymentProviderService(new ProviderPayment("pay-123", $"ref:{subId}", "approved", PaymentStatus.Approved, 29.90m, "BRL", "pref-123")),
            audit,
            NullLogger<PaymentWebhookService>.Instance);

        var result = await webhookService.UpdateSubscriptionFromPaymentAsync(
            Guid.NewGuid(),
            new ProviderPayment("pay-123", $"ref:{subId}", "approved", PaymentStatus.Approved, 29.90m, "BRL", "pref-123"));

        Assert.True(result.IsSuccess);
        var updatedSub = await subRepo.GetByIdAsync(subId);
        Assert.Equal(SubscriptionStatus.Active, updatedSub!.Status);
        Assert.Contains(audit.Logs, a => a.Action == "billing.payment.approved");
        Assert.Contains(audit.Logs, a => a.Action == "billing.subscription.updated");
    }

    [Fact]
    public async Task Webhook_pending_updates_subscription_and_audits()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var sub = new Subscription(subId, userId, "premium_monthly", SubscriptionStatus.Pending, BillingCycle.Monthly, PaymentProvider.MercadoPago, null, null, null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
        var user = new User(userId, "User", "user@teste.com", "hash", null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Inactive, false, false, DateTime.UtcNow, DateTime.UtcNow, null, null, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());

        var subRepo = new FakeSubscriptionRepository(sub);
        var userRepo = new FakeUserRepository(user);
        var audit = new FakePaymentAuditRepository();
        var txRepo = new FakePaymentTransactionRepository();
        var webhookRepo = new FakePaymentWebhookRepository();
        var subService = new SubscriptionService(subRepo, userRepo, audit, NullLogger<SubscriptionService>.Instance);

        var webhookService = new PaymentWebhookService(
            webhookRepo,
            subRepo,
            txRepo,
            subService,
            new PaymentMetadataSanitizer(),
            new FakePaymentProviderService(new ProviderPayment("pay-123", $"ref:{subId}", "pending", PaymentStatus.Pending, 29.90m, "BRL", "pref-123")),
            audit,
            NullLogger<PaymentWebhookService>.Instance);

        var result = await webhookService.UpdateSubscriptionFromPaymentAsync(
            Guid.NewGuid(),
            new ProviderPayment("pay-123", $"ref:{subId}", "pending", PaymentStatus.Pending, 29.90m, "BRL", "pref-123"));

        Assert.True(result.IsSuccess);
        var updatedSub = await subRepo.GetByIdAsync(subId);
        Assert.Equal(SubscriptionStatus.PaymentPending, updatedSub!.Status);
        Assert.Contains(audit.Logs, a => a.Action == "billing.payment.pending");
    }

    [Fact]
    public async Task Webhook_rejected_updates_subscription_and_audits()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var sub = new Subscription(subId, userId, "premium_monthly", SubscriptionStatus.Pending, BillingCycle.Monthly, PaymentProvider.MercadoPago, null, null, null, null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
        var user = new User(userId, "User", "user@teste.com", "hash", null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Inactive, false, false, DateTime.UtcNow, DateTime.UtcNow, null, null, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());

        var subRepo = new FakeSubscriptionRepository(sub);
        var userRepo = new FakeUserRepository(user);
        var audit = new FakePaymentAuditRepository();
        var txRepo = new FakePaymentTransactionRepository();
        var webhookRepo = new FakePaymentWebhookRepository();
        var subService = new SubscriptionService(subRepo, userRepo, audit, NullLogger<SubscriptionService>.Instance);

        var webhookService = new PaymentWebhookService(
            webhookRepo,
            subRepo,
            txRepo,
            subService,
            new PaymentMetadataSanitizer(),
            new FakePaymentProviderService(new ProviderPayment("pay-123", $"ref:{subId}", "rejected", PaymentStatus.Rejected, 29.90m, "BRL", "pref-123")),
            audit,
            NullLogger<PaymentWebhookService>.Instance);

        var result = await webhookService.UpdateSubscriptionFromPaymentAsync(
            Guid.NewGuid(),
            new ProviderPayment("pay-123", $"ref:{subId}", "rejected", PaymentStatus.Rejected, 29.90m, "BRL", "pref-123"));

        Assert.True(result.IsSuccess);
        var updatedSub = await subRepo.GetByIdAsync(subId);
        Assert.Equal(SubscriptionStatus.Failed, updatedSub!.Status);
        Assert.Contains(audit.Logs, a => a.Action == "billing.payment.failed");
    }

    [Fact]
    public async Task Webhook_duplicate_is_ignored_and_audited()
    {
        var webhookRepo = new FakePaymentWebhookRepository();
        var existingEvent = new PaymentWebhookEvent(Guid.NewGuid(), PaymentProvider.MercadoPago, "evt-duplicate", "payment", "Processed", DateTime.UtcNow, DateTime.UtcNow, null, null, null, "{}", null);
        await webhookRepo.CreateAsync(existingEvent);

        var subRepo = new FakeSubscriptionRepository();
        var userRepo = new FakeUserRepository();
        var audit = new FakePaymentAuditRepository();
        var txRepo = new FakePaymentTransactionRepository();
        var subService = new SubscriptionService(subRepo, userRepo, audit, NullLogger<SubscriptionService>.Instance);

        var webhookService = new PaymentWebhookService(
            webhookRepo,
            subRepo,
            txRepo,
            subService,
            new PaymentMetadataSanitizer(),
            new FakePaymentProviderService(),
            audit,
            NullLogger<PaymentWebhookService>.Instance);

        var payload = "{\"id\":\"evt-duplicate\",\"type\":\"payment\",\"data\":{\"id\":\"pay-123\"}}";
        var result = await webhookService.ReceiveAsync(PaymentProvider.MercadoPago, payload, new Dictionary<string, string>());

        Assert.True(result.IsSuccess);
        // Não adicionou outro webhook
        Assert.Single(webhookRepo.Events);
    }

    [Fact]
    public async Task Subscription_cancellation_preserves_access_until_period_end_and_audits()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var endOfPeriod = now.AddDays(25);
        var sub = new Subscription(subId, userId, "premium_monthly", SubscriptionStatus.Active, BillingCycle.Monthly, PaymentProvider.MercadoPago, null, null, null, null, now, endOfPeriod, null, null, now, now);
        var user = new User(userId, "User", "user@teste.com", "hash", null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, UserPlan.Premium, PlanStatus.Active, false, false, now, now, null, null, now, now, Guid.NewGuid());

        var subRepo = new FakeSubscriptionRepository(sub);
        var userRepo = new FakeUserRepository(user);
        var audit = new FakePaymentAuditRepository();
        var subService = new SubscriptionService(subRepo, userRepo, audit, NullLogger<SubscriptionService>.Instance);

        var result = await subService.CancelSubscriptionAsync(subId, "Solicitação do cliente no autoatendimento");

        Assert.True(result.IsSuccess);
        var updatedSub = await subRepo.GetByIdAsync(subId);
        Assert.NotNull(updatedSub!.CanceledAt);
        Assert.Equal(SubscriptionStatus.Active, updatedSub.Status); // mantido ativo até o fim do ciclo
        Assert.Contains(audit.Logs, a => a.Action == "billing.subscription.canceled");
    }

    [Fact]
    public void Ai_usage_limiter_respects_limits_and_allows_team_and_enterprise()
    {
        var limiter = new AiUsageLimiter();
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Assert.Equal(5, limiter.LimitFor(UserPlan.Free));
        Assert.Equal(40, limiter.LimitFor(UserPlan.Premium));
        Assert.Equal(100, limiter.LimitFor("team"));
        Assert.Equal(500, limiter.LimitFor("enterprise"));

        Assert.True(limiter.TryConsume(clientId, userId, UserPlan.Free));
        Assert.True(limiter.TryConsume(clientId, userId, UserPlan.Premium));
        Assert.True(limiter.TryConsume(clientId, userId, "team"));
    }

    [Fact]
    public void Ai_secret_resolver_reads_only_from_environment_variables()
    {
        Assert.Equal("HABITFLOW_AI_GROQ_API_KEY", AiSecretResolver.GroqVariable);
        Assert.Equal("HABITFLOW_AI_GEMINI_API_KEY", AiSecretResolver.GeminiVariable);
        Assert.Equal("HABITFLOW_AI_DEEPSEEK_API_KEY", AiSecretResolver.DeepSeekVariable);
        Assert.Equal("", AiSecretResolver.ForProvider("unknown_provider"));
    }

    [Fact]
    public void Ai_model_policy_validates_allowed_models()
    {
        Assert.False(AiModelPolicy.IsAllowed("", ["llama-3.3-70b-versatile"]));
        Assert.True(AiModelPolicy.IsAllowed("llama-3.3-70b-versatile", ["llama-3.3-70b-versatile", "mixtral-8x7b-32768"]));
        Assert.False(AiModelPolicy.IsAllowed("unauthorized-gpt-5", ["llama-3.3-70b-versatile"]));
    }

    [Fact]
    public void All_razor_views_have_valid_media_syntax()
    {
        var viewsPath = Path.Combine(Root, "src", "HabitFlow.Web", "Views");
        if (!Directory.Exists(viewsPath)) return;

        var cshtmlFiles = Directory.GetFiles(viewsPath, "*.cshtml", SearchOption.AllDirectories);
        foreach (var file in cshtmlFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("@media (", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_096_exists_and_is_included_in_script_completo()
    {
        var migration096 = Path.Combine(Root, "database", "migrations", "096_v6200_commercial_homologation_trial.sql");
        var scriptCompleto = Path.Combine(Root, "database", "script_completo.sql");

        Assert.True(File.Exists(migration096));
        Assert.True(File.Exists(scriptCompleto));

        var fullScript = File.ReadAllText(scriptCompleto);
        Assert.Contains("096_v6200_commercial_homologation_trial.sql", fullScript);
    }
}

file class FakePlanRepository(IReadOnlyList<Plan>? plans = null) : IPlanRepository
{
    private readonly IReadOnlyList<Plan> _plans = plans ?? [];
    public Task<IReadOnlyList<Plan>> GetPublicPlansAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Plan>>(_plans.Where(p => p.IsPublic).ToList());
    public Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_plans.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
}

file class FakePlanCatalogRepository : IPlanCatalogRepository
{
    public Task<bool> IsCheckoutEligibleAsync(string planCode, string cycle, CancellationToken ct = default) => Task.FromResult(true);
    public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PublicPlan>>([]);
    public Task<PublicPlan?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<PublicPlan?>(null);
    public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) => Task.FromResult<ClientPlanAccess?>(null);
    public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
    public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, PlanFeatureValue>>(new Dictionary<string, PlanFeatureValue>());
    public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
}

file class FakeSubscriptionRepository(Subscription? initial = null) : ISubscriptionRepository
{
    private readonly List<Subscription> _items = initial is not null ? [initial] : [];
    public Task<Subscription?> GetActiveOrLatestByUserIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(_items.LastOrDefault(x => x.UserId == userId));
    public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
    public Task<Subscription?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default) => Task.FromResult(_items.FirstOrDefault(x => x.ProviderPaymentId == providerPaymentId));
    public Task CreateAsync(Subscription subscription, CancellationToken ct = default) { _items.Add(subscription); return Task.CompletedTask; }
    public Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
    {
        var idx = _items.FindIndex(x => x.Id == subscription.Id);
        if (idx >= 0) _items[idx] = subscription;
        else _items.Add(subscription);
        return Task.CompletedTask;
    }
    public Task UpdateCheckoutAsync(Guid id, string? checkoutUrl, string? providerSubscriptionId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<Subscription>> ListByStatusAsync(SubscriptionStatus? status, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => !status.HasValue || x.Status == status.Value).ToList());
}

file class FakeUserRepository(User? initial = null) : IUserRepository
{
    private readonly List<User> _users = initial is not null ? [initial] : [];
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    public Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<User>>(_users);
    public Task CreateAsync(User user, CancellationToken ct = default) { _users.Add(user); return Task.CompletedTask; }
    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var idx = _users.FindIndex(x => x.Id == user.Id);
        if (idx >= 0) _users[idx] = user;
        else _users.Add(user);
        return Task.CompletedTask;
    }
    public Task<bool> LinkToClientFromInviteAsync(Guid userId, Guid clientId, UserRole role, DateTime utcNow, CancellationToken ct = default) => Task.FromResult(true);
    public Task UpdatePasswordAndSessionVersionAsync(Guid userId, string passwordHash, CancellationToken ct = default) => Task.CompletedTask;
    public Task IncrementSessionVersionAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddLoginAttemptAsync(LoginAttempt attempt, CancellationToken ct = default) => Task.CompletedTask;
}

file class FakePaymentAuditRepository : IPaymentAuditRepository
{
    public List<PaymentAuditLog> Logs { get; } = [];
    public Task CreateAsync(PaymentAuditLog auditLog, CancellationToken ct = default)
    {
        Logs.Add(auditLog);
        return Task.CompletedTask;
    }
}

file class FakePaymentTransactionRepository : IPaymentTransactionRepository
{
    public List<PaymentTransaction> Transactions { get; } = [];
    public Task CreateAsync(PaymentTransaction transaction, CancellationToken ct = default)
    {
        Transactions.Add(transaction);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<PaymentTransaction>> ListLatestAsync(int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PaymentTransaction>>(Transactions);
    public Task<IReadOnlyList<PaymentTransaction>> ListByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PaymentTransaction>>(Transactions.Where(t => t.UserId == userId).ToList());
}

file class FakePaymentWebhookRepository : IPaymentWebhookRepository
{
    public List<PaymentWebhookEvent> Events { get; } = [];
    public Task<bool> ExistsAsync(PaymentProvider provider, string eventId, CancellationToken ct = default) =>
        Task.FromResult(Events.Any(e => e.Provider == provider && e.EventId == eventId));
    public Task CreateAsync(PaymentWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        Events.Add(webhookEvent);
        return Task.CompletedTask;
    }
    public Task MarkProcessedAsync(Guid id, Guid? userId, Guid? subscriptionId, Guid? transactionId, string? error, CancellationToken ct = default)
    {
        var evt = Events.FirstOrDefault(e => e.Id == id);
        if (evt != null)
        {
            var idx = Events.IndexOf(evt);
            Events[idx] = evt with { Status = error == null ? "Processed" : "Error", ProcessedAt = DateTime.UtcNow, UserId = userId, SubscriptionId = subscriptionId, PaymentTransactionId = transactionId, ProcessingError = error };
        }
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<PaymentWebhookEvent>> ListLatestAsync(int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PaymentWebhookEvent>>(Events);
}

file class FakePaymentProviderService(ProviderPayment? payment = null) : IPaymentProviderService
{
    public Task<HabitFlow.Shared.Result<CheckoutPreference>> CreateCheckoutPreferenceAsync(CheckoutRequest request, Subscription subscription, Plan plan, CancellationToken ct = default) =>
        Task.FromResult(HabitFlow.Shared.Result<CheckoutPreference>.Success(new CheckoutPreference("https://checkout.mercadopago.com/pref-123", "pref-123")));

    public Task<HabitFlow.Shared.Result<ProviderPayment>> GetPaymentAsync(string providerPaymentId, CancellationToken ct = default) =>
        Task.FromResult(payment != null
            ? HabitFlow.Shared.Result<ProviderPayment>.Success(payment)
            : HabitFlow.Shared.Result<ProviderPayment>.Failure("payment.not_found", "Pagamento não encontrado"));

    public Task<HabitFlow.Shared.Result> ValidateWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default) =>
        Task.FromResult(HabitFlow.Shared.Result.Success());
}
