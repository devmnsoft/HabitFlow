using System.Text.Json;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PwaPushV6168Tests
{
    private static string Root => RepositoryRootLocator.Root.ToString();

    [Fact] public void Manifest_IsInstallableAndHasProfessionalMetadata()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/wwwroot/manifest.webmanifest")));
        var root=json.RootElement;
        foreach(var key in new[]{"name","short_name","description","start_url","scope","display","background_color","theme_color","icons","shortcuts","categories","lang"}) Assert.True(root.TryGetProperty(key,out _),key);
        Assert.Contains(root.GetProperty("icons").EnumerateArray(),x=>x.GetProperty("sizes").GetString()=="192x192");
        Assert.Contains(root.GetProperty("icons").EnumerateArray(),x=>x.GetProperty("sizes").GetString()=="512x512"&&x.GetProperty("purpose").GetString()=="maskable");
    }

    [Fact] public void ServiceWorker_DoesNotCacheSensitiveRoutes()
    {
        var sw=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/wwwroot/service-worker.js"));
        Assert.Contains("SENSITIVE",sw); Assert.Contains("cache:'no-store'",sw); Assert.DoesNotContain("'/account'",sw.Split("const STATIC=")[1].Split(';')[0]);
    }

    [Fact] public void PushEndpoints_AreAuthenticatedAndTenantScoped()
    {
        var controller=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/Controllers/NotificationsController.cs"));
        var repository=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Infrastructure/Repositories/PushSubscriptionRepository.cs"));
        Assert.Contains("[Authorize]",controller); Assert.Contains("CurrentClientId()",controller); Assert.Contains("client_id=@clientId and user_id=@userId",repository);
    }

    [Fact] public void Migration_IsAdditiveAndComplete()
    {
        var sql=File.ReadAllText(Path.Combine(Root,"database/migrations/072_v6168_pwa_push_offline.sql"));
        foreach(var table in new[]{"push_subscriptions","notification_preferences","push_delivery_attempts","offline_sync_events"}) Assert.Contains($"create table if not exists habitflow.{table}",sql);
    }
}
