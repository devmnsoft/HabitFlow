using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Options;
using Xunit;

namespace HabitFlow.Tests;
public sealed class PwaNotificationsV6177Tests
{
    private static string Read(string path) => File.ReadAllText(RepositoryRootLocator.PathTo(path));
    [Fact] public void Manifest_IsStandaloneAndVersionedWorkerHasSafeOfflineFallbacks()
    {
        using var json = JsonDocument.Parse(Read("src/HabitFlow.Web/wwwroot/manifest.webmanifest"));
        Assert.Equal("standalone", json.RootElement.GetProperty("display").GetString());
        var sw = Read("src/HabitFlow.Web/wwwroot/service-worker.js");
        Assert.Contains("v6.17.7", sw); Assert.Contains("offline-private.html", sw);
        Assert.Contains("cache: 'no-store'", sw); Assert.DoesNotContain("'/dashboard'", sw.Split("const STATIC =")[1].Split(';')[0]);
    }
    [Fact] public async Task Preferences_RejectInvalidTimezoneAndQuietPeriod()
    {
        var repository = new PreferenceRepository(); var service = new PushNotificationPreferenceService(repository);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(new(Guid.NewGuid(),Guid.NewGuid(),false,true,new(22,0),null,5,null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(new(Guid.NewGuid(),Guid.NewGuid(),false,true,null,null,5,null,Timezone:"Invalid/Zone")));
    }
    [Fact] public async Task Preferences_AcceptValidTenantScopedValues()
    {
        var repository = new PreferenceRepository(); var service = new PushNotificationPreferenceService(repository); var client=Guid.NewGuid(); var user=Guid.NewGuid();
        await service.SaveAsync(new(client,user,false,true,new(22,0),new(7,0),5,null,Timezone:"UTC",Language:"fr-FR"));
        Assert.Equal(client, repository.Saved!.ClientId); Assert.Equal(user, repository.Saved.UserId);
        Assert.Equal("fr-FR", repository.Saved.Language);
    }
    [Fact] public async Task PushEndpointSafety_BlocksLocalDestinations()
    {
        Assert.False(await PushEndpointSafetyPolicy.IsAllowedAsync("https://127.0.0.1/push"));
        Assert.False(await PushEndpointSafetyPolicy.IsAllowedAsync("https://169.254.169.254/latest/meta-data"));
        Assert.False(await PushEndpointSafetyPolicy.IsAllowedAsync("https://localhost/push"));
    }
    [Fact] public async Task SendSafeReminder_StopsWhenDailyLogicalLimitIsReached()
    {
        var client = Guid.NewGuid(); var user = Guid.NewGuid();
        var repository = new PreferenceRepository { QuotaAllowed = false };
        repository.Subscriptions.Add(new(Guid.NewGuid(),client,user,"https://1.1.1.1/push",Token(),Token(),"Teste",true,DateTime.UtcNow,DateTime.UtcNow));
        var sender = new FakeSender(true);
        var service = new PushNotificationService(repository, sender, Options.Create(EnabledPushOptions()), new FixedTimeProvider());

        var sent = await service.SendSafeReminderAsync(client,user,new(Guid.NewGuid(),new DateTimeOffset(2026,8,1,12,0,0,TimeSpan.Zero)));

        Assert.Equal(0, sent);
        Assert.Equal(0, sender.Calls);
    }
    [Fact] public async Task SendSafeReminder_RecordsProviderAcceptedWithoutDeliveredClaim()
    {
        var client = Guid.NewGuid(); var user = Guid.NewGuid();
        var repository = new PreferenceRepository();
        repository.Subscriptions.Add(new(Guid.NewGuid(),client,user,"https://1.1.1.1/push",Token(),Token(),"Teste",true,DateTime.UtcNow,DateTime.UtcNow));
        var service = new PushNotificationService(repository, new FakeSender(true), Options.Create(EnabledPushOptions()), new FixedTimeProvider());

        var sent = await service.SendSafeReminderAsync(client,user,new(Guid.NewGuid(),new DateTimeOffset(2026,8,1,12,0,0,TimeSpan.Zero)));

        Assert.Equal(1, sent);
        Assert.Contains(repository.CompletedStatuses, x => x == "ProviderAccepted");
        Assert.DoesNotContain("Delivered", repository.CompletedStatuses);
    }
    private sealed class PreferenceRepository : IPushSubscriptionRepository
    {
        public PushNotificationPreference? Saved {get;private set;}
        public bool QuotaAllowed { get; init; } = true;
        public List<PushSubscriptionRecord> Subscriptions { get; } = [];
        public List<string> CompletedStatuses { get; } = [];
        public Task SavePreferenceAsync(PushNotificationPreference value,CancellationToken ct=default){Saved=value;return Task.CompletedTask;}
        public Task<PushNotificationPreference> GetPreferenceAsync(Guid c,Guid u,CancellationToken ct=default)=>Task.FromResult(new PushNotificationPreference(c,u,true,true,null,null,1,null));
        public Task UpsertAsync(PushSubscriptionRecord s,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid c,Guid u,bool activeOnly=false,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<PushSubscriptionRecord>>(Subscriptions);
        public Task<bool> RemoveAsync(Guid c,Guid u,Guid id,CancellationToken ct=default)=>Task.FromResult(false);
        public Task DeactivateAsync(Guid c,Guid u,Guid id,CancellationToken ct=default)=>Task.CompletedTask;
        public Task RecordAttemptAsync(PushDeliveryAttempt a,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<bool> TryReserveDailyQuotaAsync(Guid c,Guid u,DateOnly d,string ch,int max,CancellationToken ct=default)=>Task.FromResult(QuotaAllowed);
        public Task ReleaseDailyQuotaAsync(Guid c,Guid u,DateOnly d,string ch,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<bool> TryBeginAttemptAsync(PushDeliveryAttempt a,CancellationToken ct=default)=>Task.FromResult(true);
        public Task CompleteAttemptAsync(Guid c,Guid u,Guid id,string status,string? errorCode,DateTime attemptedAt,CancellationToken ct=default){CompletedStatuses.Add(status);return Task.CompletedTask;}
    }
    private sealed class FakeSender(bool succeeds) : IPushNotificationSender
    {
        public int Calls { get; private set; }
        public Task<PushSendResult> SendAsync(PushSubscriptionRecord subscription, PushPayload payload, CancellationToken ct=default)
        {
            Calls++;
            return Task.FromResult(new PushSendResult(succeeds,false,succeeds ? null : "provider_error"));
        }
    }
    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026,8,1,12,0,0,TimeSpan.Zero);
    }
    private static PushNotificationOptions EnabledPushOptions() => new() { Enabled = true, PublicKey = Token(), PrivateKey = Token() };
    private static string Token() => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
