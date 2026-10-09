using System.Security.Cryptography;
using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6250PublicApiAndAiTests
{
    [Fact]
    public void ApiKey_HashGeneration_IsDeterministicSha256()
    {
        var rawKey = "hf_live_sample_key_123456789";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        Assert.Equal(64, hex.Length);
        Assert.Matches("^[0-9a-f]{64}$", hex);
    }

    [Fact]
    public void IntegrationScopes_ContainBothColonAndDotNotation()
    {
        Assert.Contains("habits:read", IntegrationScopes.Allowed);
        Assert.Contains("habits.read", IntegrationScopes.Allowed);
        Assert.Contains("habits:write", IntegrationScopes.Allowed);
        Assert.Contains("checkins:write", IntegrationScopes.Allowed);
        Assert.Contains("webhooks:manage", IntegrationScopes.Allowed);
    }

    [Fact]
    public void AiMobileIntegration_SanitizesSensitiveInputs()
    {
        var rawInput = "Meu e-mail secreto é admin@empresa.com e meu token é hf_live_123456789abc";
        var sanitized = AiMobileIntegrationService.SanitizeInput(rawInput);

        Assert.DoesNotContain("admin@empresa.com", sanitized);
        Assert.DoesNotContain("hf_live_123456789abc", sanitized);
        Assert.Contains("[EMAIL_PROTEGIDO]", sanitized);
        Assert.Contains("[TOKEN_PROTEGIDO]", sanitized);
    }

    [Fact]
    public async Task AiMobileIntegration_SuggestOptimalReminderTime_DeterministicFallback()
    {
        var options = Options.Create(new AiOptions
        {
            Enabled = false
        });

        var completions = new CompletionRepoMock();
        var habitId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var habit = new Habit(habitId, userId, "Beber água", "#10B981", "Saúde", false, null, DateTime.UtcNow, DateTime.UtcNow, HabitFrequencyType.Daily, null, null, null, 0, clientId);

        var habits = new HabitRepoMock([habit]);
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();

        var aiService = new AiMobileIntegrationService(options, completions, habits, entitlements, audit, NullLogger<AiMobileIntegrationService>.Instance);

        var time = await aiService.SuggestBestReminderTimeAsync(clientId, userId, habitId);

        Assert.NotNull(time);
        Assert.Matches(@"^\d{2}:\d{2}$", time);
    }

    [Fact]
    public void NotificationDispatcher_ChecksQuietHours()
    {
        var isQuiet = NotificationEventDispatcher.IsInQuietHours(new TimeOnly(23, 30), new TimeOnly(22, 0), new TimeOnly(7, 0));
        Assert.True(isQuiet, "23:30 deve ser considerado dentro do horário de silêncio (22h - 07h)");

        var isNotQuiet = NotificationEventDispatcher.IsInQuietHours(new TimeOnly(14, 0), new TimeOnly(22, 0), new TimeOnly(7, 0));
        Assert.False(isNotQuiet, "14:00 não deve ser horário de silêncio");
    }

    [Fact]
    public void NotificationEventTypes_HasAllCanonicalSaaSTypes()
    {
        Assert.Equal("habit_reminder", NotificationEventTypes.HabitReminder);
        Assert.Equal("weekly_review_available", NotificationEventTypes.WeeklyReviewAvailable);
        Assert.Equal("consistency_drop", NotificationEventTypes.ConsistencyDrop);
        Assert.Equal("goal_completed", NotificationEventTypes.GoalCompleted);
        Assert.Equal("trial_expiring", NotificationEventTypes.TrialExpiring);
        Assert.Equal("trial_expired", NotificationEventTypes.TrialExpired);
        Assert.Equal("payment_pending", NotificationEventTypes.PaymentPending);
        Assert.Equal("subscription_active", NotificationEventTypes.SubscriptionActive);
        Assert.Equal("ticket_replied", NotificationEventTypes.TicketReplied);
        Assert.Equal("invite_received", NotificationEventTypes.InviteReceived);
        Assert.Equal("plan_limit_reached", NotificationEventTypes.PlanLimitReached);
        Assert.Equal("ai_unavailable", NotificationEventTypes.AiUnavailable);
        Assert.Equal("offline_sync_error", NotificationEventTypes.OfflineSyncError);
    }
}
