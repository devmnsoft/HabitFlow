using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
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
        await service.SaveAsync(new(client,user,false,true,new(22,0),new(7,0),5,null,Timezone:"UTC"));
        Assert.Equal(client, repository.Saved!.ClientId); Assert.Equal(user, repository.Saved.UserId);
    }
    private sealed class PreferenceRepository : IPushSubscriptionRepository
    {
        public PushNotificationPreference? Saved {get;private set;}
        public Task SavePreferenceAsync(PushNotificationPreference value,CancellationToken ct=default){Saved=value;return Task.CompletedTask;}
        public Task<PushNotificationPreference> GetPreferenceAsync(Guid c,Guid u,CancellationToken ct=default)=>Task.FromResult(new PushNotificationPreference(c,u,false,true,null,null,5,null));
        public Task UpsertAsync(PushSubscriptionRecord s,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid c,Guid u,bool activeOnly=false,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<PushSubscriptionRecord>>([]);
        public Task<bool> RemoveAsync(Guid c,Guid u,Guid id,CancellationToken ct=default)=>Task.FromResult(false);
        public Task DeactivateAsync(Guid c,Guid u,Guid id,CancellationToken ct=default)=>Task.CompletedTask;
        public Task RecordAttemptAsync(PushDeliveryAttempt a,CancellationToken ct=default)=>Task.CompletedTask;
    }
}
