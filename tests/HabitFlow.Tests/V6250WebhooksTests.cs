using System.Security.Cryptography;
using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6250WebhooksTests
{
    [Fact]
    public void HmacSignature_MatchesStandardSha256()
    {
        var secret = "test_webhook_secret_key_12345";
        var payload = "{\"event\":\"habit.created\",\"tenantId\":\"11111111-1111-1111-1111-111111111111\"}";

        var expectedHex = WebhookDispatcherService.ComputeHmacSha256(secret, payload);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var manualHex = Convert.ToHexString(hash).ToLowerInvariant();

        Assert.Equal(64, expectedHex.Length);
        Assert.Equal(manualHex, expectedHex);
    }

    [Fact]
    public async Task DispatchEventAsync_FiltersByEventAndLogsAttempt()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var webhook = new IntegrationWebhook(
            Id: Guid.NewGuid(),
            ClientId: clientId,
            UserId: userId,
            Name: "Zapier Sync",
            Url: "https://example.com/webhook",
            Events: [WebhookEvents.HabitCreated, WebhookEvents.HabitCompleted],
            SecretCiphertext: "secret_123",
            Enabled: true,
            CreatedAt: DateTime.UtcNow,
            LastSuccessAt: null,
            ConsecutiveFailures: 0,
            LastFailedAt: null,
            IsPaused: false
        );

        var repo = new MockIntegrationRepo([webhook]);
        var entitlements = new PlanEntitlementService(new MockPlanCatalogRepo());
        var handler = new MockHttpMessageHandler(success: true);
        var httpClient = new HttpClient(handler);

        var dispatcher = new WebhookDispatcherService(repo, entitlements, httpClient, NullLogger<WebhookDispatcherService>.Instance);

        var dispatched = await dispatcher.DispatchEventAsync(clientId, userId, WebhookEvents.HabitCreated, new { habit_id = Guid.NewGuid(), name = "Treinar" });

        Assert.Equal(1, dispatched);
        Assert.Single(repo.DeliveryAttempts);
        Assert.Equal("Success", repo.DeliveryAttempts[0].Status);
        Assert.Equal(200, repo.DeliveryAttempts[0].ResponseCode);
    }

    [Fact]
    public async Task DispatchEventAsync_RepeatedFailures_AutoPausesEndpoint()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var webhook = new IntegrationWebhook(
            Id: Guid.NewGuid(),
            ClientId: clientId,
            UserId: userId,
            Name: "Failing Hook",
            Url: "https://example.com/failing",
            Events: [WebhookEvents.HabitCreated],
            SecretCiphertext: "secret_fail",
            Enabled: true,
            CreatedAt: DateTime.UtcNow,
            LastSuccessAt: null,
            ConsecutiveFailures: 4, // 5th failure triggers pause
            LastFailedAt: null,
            IsPaused: false
        );

        var repo = new MockIntegrationRepo([webhook]);
        var entitlements = new PlanEntitlementService(new MockPlanCatalogRepo());
        var handler = new MockHttpMessageHandler(success: false);
        var httpClient = new HttpClient(handler);

        var dispatcher = new WebhookDispatcherService(repo, entitlements, httpClient, NullLogger<WebhookDispatcherService>.Instance);

        var dispatched = await dispatcher.DispatchEventAsync(clientId, userId, WebhookEvents.HabitCreated, new { test = true });

        Assert.Equal(0, dispatched);
        Assert.Single(repo.DeliveryAttempts);
        Assert.Equal("Failed", repo.DeliveryAttempts[0].Status);
        Assert.True(repo.PausedWebhooks.Contains(webhook.Id), "Webhook deve ser pausado automaticamente após 5 falhas consecutivas");
    }

    private sealed class MockHttpMessageHandler(bool success) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(success ? "{\"received\":true}" : "Server Error")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class MockPlanCatalogRepo : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PublicPlan>>([]);
        public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
            Task.FromResult<ClientPlanAccess?>(new ClientPlanAccess(clientId, "pro", "pro", "active", null, null));
        public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<Guid?>(Guid.NewGuid());
        public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, PlanFeatureValue>>(new Dictionary<string, PlanFeatureValue>
            {
                [PlanFeatureCodes.Webhooks] = new(PlanFeatureCodes.Webhooks, "Webhooks", "boolean", true, null, null),
                [PlanFeatureCodes.PublicApi] = new(PlanFeatureCodes.PublicApi, "Public API", "boolean", true, null, null)
            });
        public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
    }

    private sealed class MockIntegrationRepo(IReadOnlyList<IntegrationWebhook> webhooks) : IIntegrationRepository
    {
        public List<WebhookDeliveryAttemptRecord> DeliveryAttempts { get; } = [];
        public HashSet<Guid> PausedWebhooks { get; } = [];

        public Task<ApiKeyRecord?> FindApiKeyAsync(string hash, CancellationToken ct = default) => Task.FromResult<ApiKeyRecord?>(null);
        public Task<IReadOnlyList<ApiKeyRecord>> ListApiKeysAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ApiKeyRecord>>([]);
        public Task CreateApiKeyAsync(ApiKeyRecord key, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> RenameApiKeyAsync(Guid clientId, Guid userId, Guid id, string name, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> RevokeApiKeyAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task TouchApiKeyAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

        public Task<CalendarFeed?> GetCalendarFeedAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<CalendarFeed?>(null);
        public Task<CalendarFeed?> FindCalendarFeedAsync(string tokenHash, CancellationToken ct = default) => Task.FromResult<CalendarFeed?>(null);
        public Task UpsertCalendarFeedAsync(CalendarFeed feed, CancellationToken ct = default) => Task.CompletedTask;
        public Task TouchCalendarFeedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<IntegrationWebhook>> ListWebhooksAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(webhooks);
        public Task<IntegrationWebhook?> GetWebhookByIdAsync(Guid clientId, Guid id, CancellationToken ct = default) =>
            Task.FromResult(webhooks.FirstOrDefault(w => w.Id == id));
        public Task<IReadOnlyList<IntegrationWebhook>> ListActiveWebhooksForEventAsync(Guid clientId, string eventName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<IntegrationWebhook>>(webhooks.Where(w => w.Enabled && !w.IsPaused && w.Events.Contains(eventName)).ToList());
        public Task CreateWebhookAsync(IntegrationWebhook webhook, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ToggleWebhookAsync(Guid clientId, Guid userId, Guid id, bool enabled, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteWebhookAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task UpdateWebhookSuccessAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateWebhookFailureAsync(Guid id, bool autoPause, CancellationToken ct = default)
        {
            if (autoPause) PausedWebhooks.Add(id);
            return Task.CompletedTask;
        }
        public Task RecordDeliveryAttemptAsync(WebhookDeliveryAttemptRecord attempt, CancellationToken ct = default)
        {
            DeliveryAttempts.Add(attempt);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<WebhookDeliveryAttemptRecord>> ListDeliveryAttemptsAsync(Guid clientId, Guid webhookId, int limit = 20, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WebhookDeliveryAttemptRecord>>(DeliveryAttempts);
        public Task<WebhookDeliveryAttemptRecord?> GetDeliveryAttemptAsync(Guid clientId, Guid attemptId, CancellationToken ct = default) =>
            Task.FromResult(DeliveryAttempts.FirstOrDefault(a => a.Id == attemptId));
        public Task AddAuditAsync(Guid clientId, Guid userId, string eventName, object metadata, CancellationToken ct = default) => Task.CompletedTask;
    }
}
