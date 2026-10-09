using System.Text.Json;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6250PwaMobileTests
{
    private static readonly string Root = RepositoryRootLocator.Root;

    [Fact]
    public void Manifest_HasVersionedAppIdentityAndMaskableIcons()
    {
        var path = Path.Combine(Root, "src", "HabitFlow.Web", "wwwroot", "manifest.webmanifest");
        Assert.True(File.Exists(path), "manifest.webmanifest deve existir");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        Assert.Equal("HabitFlow — Hábitos, rotina e progresso", root.GetProperty("name").GetString());
        Assert.Equal("HabitFlow", root.GetProperty("short_name").GetString());
        Assert.Equal("standalone", root.GetProperty("display").GetString());
        Assert.Equal("#10B981", root.GetProperty("theme_color").GetString());
        Assert.Equal("#F8FAFC", root.GetProperty("background_color").GetString());

        var icons = root.GetProperty("icons").EnumerateArray().ToList();
        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "192x192");
        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "512x512");
        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "512x512" && i.GetProperty("purpose").GetString() == "maskable");
    }

    [Fact]
    public void ServiceWorker_HasV6250VersionAndProtectsPrivateAndSensitiveRoutes()
    {
        var path = Path.Combine(Root, "src", "HabitFlow.Web", "wwwroot", "service-worker.js");
        var sw = File.ReadAllText(path);

        Assert.Contains("v6.25.0", sw);
        Assert.Contains("/offline.html", sw);
        Assert.Contains("/offline-private.html", sw);
        Assert.Contains("/js/offline-sync.js", sw);
        Assert.Contains("cache: 'no-store'", sw);
        Assert.Contains("PRIVATE_ROUTE", sw);
        Assert.Contains("SENSITIVE_ROUTES", sw);
        Assert.Contains("NEVER_INTERCEPT", sw);
    }

    [Fact]
    public void OfflinePages_RenderSemanticMessagesAndNoWhiteScreen()
    {
        var offlinePublic = Path.Combine(Root, "src", "HabitFlow.Web", "wwwroot", "offline.html");
        var offlinePrivate = Path.Combine(Root, "src", "HabitFlow.Web", "wwwroot", "offline-private.html");

        Assert.True(File.Exists(offlinePublic));
        Assert.True(File.Exists(offlinePrivate));

        var publicHtml = File.ReadAllText(offlinePublic);
        var privateHtml = File.ReadAllText(offlinePrivate);

        Assert.Contains("Você está sem conexão agora", publicHtml);
        Assert.Contains("Voltar ao início", publicHtml);
        Assert.Contains("Conecte-se para acessar esta área", privateHtml);
        Assert.Contains("Tentar novamente", privateHtml);
    }

    [Fact]
    public void OfflineSyncScript_ProvidesCacheClearingAndQueueMethods()
    {
        var scriptPath = Path.Combine(Root, "src", "HabitFlow.Web", "wwwroot", "js", "offline-sync.js");
        Assert.True(File.Exists(scriptPath));

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("clearQueue", script);
        Assert.Contains("getQueue", script);
        Assert.Contains("enqueue", script);
        Assert.Contains("syncNow", script);
    }
}
