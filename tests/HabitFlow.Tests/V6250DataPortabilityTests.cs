using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6250DataPortabilityTests
{
    [Fact]
    public async Task ExportUserDataAsync_ProducesCompliantJsonAndCsv()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = new DataPortabilityRepoMock();
        var habits = new HabitRepoMock();
        var completions = new CompletionRepoMock();
        var goals = new GoalRepoMock();
        var reviews = new ReviewRepoMock();
        var notifs = new NotificationRepoMock();
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();

        var service = new DataPortabilityService(repo, habits, completions, goals, reviews, notifs, entitlements, audit, NullLogger<DataPortabilityService>.Instance);

        var (jsonBytes, jsonType, jsonFile) = await service.ExportUserDataAsync(clientId, userId, "user@test.com", "Test User", "json");
        var (csvBytes, csvType, csvFile) = await service.ExportUserDataAsync(clientId, userId, "user@test.com", "Test User", "csv");

        Assert.NotEmpty(jsonBytes);
        Assert.NotEmpty(csvBytes);
        Assert.Contains(".json", jsonFile);
        Assert.Contains(".csv", csvFile);

        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

        Assert.Contains(userId.ToString(), jsonString);
        Assert.Contains("Habits", jsonString);
        Assert.Contains("Nome,Categoria,Frequencia", csvString);
    }

    [Fact]
    public async Task SimulateHabitsCsvAsync_ValidatesRowsAndDetectsErrors()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = new DataPortabilityRepoMock();
        var habits = new HabitRepoMock();
        var completions = new CompletionRepoMock();
        var goals = new GoalRepoMock();
        var reviews = new ReviewRepoMock();
        var notifs = new NotificationRepoMock();
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();

        var service = new DataPortabilityService(repo, habits, completions, goals, reviews, notifs, entitlements, audit, NullLogger<DataPortabilityService>.Instance);

        var csv = "Nome,Categoria,Frequencia\nBeber 2L de água,Saúde,Daily\n,SemNome,Daily\nMeditar 10m,Mente,Daily";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var simulation = await service.SimulateHabitsCsvAsync(clientId, userId, stream);

        Assert.Equal(3, simulation.TotalRows);
        Assert.Equal(2, simulation.ValidRows);
        Assert.Equal(1, simulation.InvalidRows);
        Assert.Single(simulation.Errors);
    }

    [Fact]
    public async Task ExecuteHabitsImportAsync_ImportsValidRows()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repo = new DataPortabilityRepoMock();
        var habits = new HabitRepoMock();
        var completions = new CompletionRepoMock();
        var goals = new GoalRepoMock();
        var reviews = new ReviewRepoMock();
        var notifs = new NotificationRepoMock();
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();

        var service = new DataPortabilityService(repo, habits, completions, goals, reviews, notifs, entitlements, audit, NullLogger<DataPortabilityService>.Instance);

        var items = new List<HabitImportRow>
        {
            new("Alongamento matinal", "Saúde", "Daily", null, 7),
            new("Caminhada 30m", "Saúde", "Weekly", null, 3)
        };

        var result = await service.ExecuteHabitsImportAsync(clientId, userId, items);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(2, habits.Habits.Count);
        Assert.Contains(habits.Habits, h => h.Name == "Alongamento matinal");
        Assert.Contains(habits.Habits, h => h.Name == "Caminhada 30m");
    }
}
