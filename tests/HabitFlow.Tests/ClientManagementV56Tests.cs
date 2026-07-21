using Xunit;

namespace HabitFlow.Tests;

public sealed class ClientManagementV56Tests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(GetRoot(), path));
    private static string GetRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "README.md"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    [Fact]
    public void ClientRepositoryUsesHabitflowSchema()
    {
        var sql = Read("src/HabitFlow.Infrastructure/Repositories/ClientRepository.cs");
        Assert.Contains("from habitflow.clients", sql);
        Assert.Contains("insert into habitflow.clients", sql);
        Assert.Contains("update habitflow.clients", sql);
    }

    [Fact]
    public void AdminClientsControllerRequiresAdminAndFeedback()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/AdminClientsController.cs");
        Assert.Contains("[Authorize(Roles = \"Admin\")]", controller);
        Assert.Contains("[ValidateAntiForgeryToken]", controller);
        Assert.Contains("Cliente cadastrado com sucesso.", controller);
        Assert.Contains("SetDatabaseError", controller);
    }

    [Fact]
    public void FeedbackBridgeAndMnsoftBadgeAreSafe()
    {
        var bridge = Read("src/HabitFlow.Web/Views/Shared/Partials/_FeedbackBridge.cshtml");
        var badge = Read("src/HabitFlow.Web/Views/Shared/Partials/_MNSOFTBrandBadge.cshtml");
        Assert.Contains("JsonSerializer.Serialize", bridge);
        Assert.DoesNotContain("assinatura temporária", badge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Consultorias e soluções em TI.", badge);
    }

    [Fact]
    public void ClientMigrationCreatesExpectedSchemaObjects()
    {
        var migration = Read("database/migrations/021_clients_management.sql");
        Assert.Contains("create table if not exists habitflow.clients", migration);
        Assert.Contains("ix_habitflow_clients_name", migration);
        Assert.Contains("check (status in ('Active', 'Inactive', 'Blocked'))", migration);
    }
}
