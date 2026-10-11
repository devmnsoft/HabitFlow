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

        var csv = "Nome,Categoria,Frequencia,Descricao,meta_dias_semana\nBeber 2L de água,Saúde,Daily,Hidratação,7\n,SemNome,Daily,Sem nome,7\nMeditar 10m,Mente,Daily,Respirar,7";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var simulation = await service.SimulateHabitsCsvAsync(clientId, userId, stream);

        Assert.Equal(3, simulation.TotalRows);
        Assert.Equal(2, simulation.ValidRows);
        Assert.Equal(1, simulation.InvalidRows);
        Assert.Single(simulation.Errors);
        Assert.NotEqual(Guid.Empty, simulation.BatchToken);
        Assert.All(simulation.ValidItems, item => Assert.Equal(simulation.BatchToken, item.BatchToken));
    }

    [Fact]
    public async Task SimulateHabitsCsvAsync_MarksExactDuplicatesForReviewWithoutWriting()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Habit(Guid.NewGuid(), userId, "Meditar 10m", "#000", "Mente", false, null, DateTime.UtcNow, DateTime.UtcNow, ClientId: clientId);
        var repo = new DataPortabilityRepoMock();
        var habits = new HabitRepoMock([existing]);
        var completions = new CompletionRepoMock();
        var goals = new GoalRepoMock();
        var reviews = new ReviewRepoMock();
        var notifs = new NotificationRepoMock();
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();
        var service = new DataPortabilityService(repo, habits, completions, goals, reviews, notifs, entitlements, audit, NullLogger<DataPortabilityService>.Instance);
        var csv = "Nome,Categoria,Frequencia,Descricao,meta_dias_semana\nMeditar 10m,Mente,Daily,Respirar,7";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var simulation = await service.SimulateHabitsCsvAsync(clientId, userId, stream);

        var item = Assert.Single(simulation.ValidItems);
        Assert.Equal("ignore", item.Decision);
        Assert.Equal(existing.Id, item.ExistingHabitId);
        Assert.NotEmpty(item.Warnings!);
        Assert.Single(habits.Habits);
    }

    [Fact]
    public async Task ExecuteHabitsImportAsync_ImportsValidRowsIdempotently()
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

        var batch = Guid.NewGuid();
        var items = new List<HabitImportRow>
        {
            new("Alongamento matinal", "Saúde", "Daily", null, 7, BatchToken: batch, RowNumber: 2),
            new("Caminhada 30m", "Saúde", "Weekdays", null, 3, BatchToken: batch, RowNumber: 3)
        };

        var result = await service.ExecuteHabitsImportAsync(clientId, userId, items);
        var resent = await service.ExecuteHabitsImportAsync(clientId, userId, items);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.True(resent.IsSuccess);
        Assert.Equal(0, resent.Value);
        Assert.Equal(2, habits.Habits.Count);
        Assert.Contains(habits.Habits, h => h.Name == "Alongamento matinal");
        Assert.Contains(habits.Habits, h => h.Name == "Caminhada 30m");
    }

    [Fact]
    public async Task ExecuteHabitsImportAsync_UpdatesOnlyWithExpectedVersion()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var existing = new Habit(Guid.NewGuid(), userId, "Ler", "#000", "Estudo", false, null, now, now, ClientId: clientId);
        var repo = new DataPortabilityRepoMock();
        var habits = new HabitRepoMock([existing]);
        var completions = new CompletionRepoMock();
        var goals = new GoalRepoMock();
        var reviews = new ReviewRepoMock();
        var notifs = new NotificationRepoMock();
        var entitlements = new PlanEntitlementService(new FakePlanCatalogRepo());
        var audit = V6250TestStubs.CreateAudit();
        var service = new DataPortabilityService(repo, habits, completions, goals, reviews, notifs, entitlements, audit, NullLogger<DataPortabilityService>.Instance);
        var item = new HabitImportRow("Ler", "Aprendizado", "Weekdays", "Livro técnico", 5, "update", existing.Id, now, Guid.NewGuid(), 2);

        var result = await service.ExecuteHabitsImportAsync(clientId, userId, [item]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aprendizado", habits.Habits[0].Category);
        Assert.Equal(HabitFrequencyType.Weekdays, habits.Habits[0].FrequencyType);
        Assert.Equal("Livro técnico", habits.Habits[0].Notes);
    }
}
