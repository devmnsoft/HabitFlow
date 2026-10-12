using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class RoutineExperimentTests
{
    private readonly Guid clientId = Guid.NewGuid();
    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid habitId = Guid.NewGuid();

    [Fact]
    public async Task Draft_preserves_previous_configuration_and_does_not_create_checkins()
    {
        var repo = new ExperimentRepositoryFake();
        var completions = new CompletionRepositoryFake();
        var service = Service(repo, completions: completions);

        var result = await service.CreateDraftAsync(Command());

        Assert.True(result.IsSuccess);
        Assert.Equal(RoutineExperimentStatus.Draft, result.Value!.Status);
        Assert.Contains("EstimatedTimeMinutes", result.Value.PreviousConfigurationJson);
        Assert.Empty(completions.Rows);
        Assert.Contains(repo.Events, x => x.EventType == "DraftCreated");
    }

    [Fact]
    public async Task Start_rejects_overlapping_active_experiment_on_same_adjustment()
    {
        var repo = new ExperimentRepositoryFake();
        var service = Service(repo);
        var first = (await service.CreateDraftAsync(Command(name: "Teste A"))).Value!;
        Assert.True((await service.StartAsync(clientId, userId, first.Id, first.Version)).IsSuccess);
        var second = await service.CreateDraftAsync(Command(name: "Teste B"));

        Assert.True(second.IsFailure);
        Assert.Equal("experiment.adjustment_conflict", second.Error.Code);
    }

    [Fact]
    public async Task Comparison_reports_small_sample_and_avoids_causal_claims()
    {
        var repo = new ExperimentRepositoryFake();
        var completions = new CompletionRepositoryFake();
        completions.Rows.Add(new(Guid.NewGuid(), habitId, userId, new(2026, 10, 1), DateTime.UtcNow));
        completions.Rows.Add(new(Guid.NewGuid(), habitId, userId, new(2026, 10, 9), DateTime.UtcNow));
        var service = Service(repo, completions: completions);
        var experiment = (await service.CreateDraftAsync(Command(
            baselineStart: new(2026, 10, 1), baselineEnd: new(2026, 10, 2),
            testStart: new(2026, 10, 8), testEnd: new(2026, 10, 9)))).Value!;

        var comparison = await service.CompareAsync(clientId, userId, experiment.Id);

        Assert.True(comparison.IsSuccess);
        Assert.Contains(comparison.Value!.Limitations, x => x.Contains("amostra pequena", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("não prova de causa", comparison.Value.Summary);
        Assert.Equal(2, comparison.Value.Baseline.Scheduled);
        Assert.Equal(1, comparison.Value.Baseline.Completed);
    }

    [Fact]
    public async Task Close_is_idempotent_and_records_private_learning_without_restoring_automatically()
    {
        var repo = new ExperimentRepositoryFake();
        var service = Service(repo);
        var experiment = (await service.CreateDraftAsync(Command())).Value!;
        var started = (await service.StartAsync(clientId, userId, experiment.Id, experiment.Version)).Value!;

        var restore = await service.CloseAsync(clientId, userId, started.Id, RoutineExperimentDecision.RestorePrevious,
            RoutineExperimentLearningOutcome.Inconclusive, "Ainda preciso revisar.", started.Version);
        var closed = await service.CloseAsync(clientId, userId, started.Id, RoutineExperimentDecision.ReviewManually,
            RoutineExperimentLearningOutcome.Inconclusive, "Ainda preciso revisar.", started.Version);
        var replay = await service.CloseAsync(clientId, userId, started.Id, RoutineExperimentDecision.KeepCurrent,
            null, null, closed.Value!.Version);

        Assert.True(restore.IsFailure);
        Assert.Equal("experiment.restore_requires_manual_review", restore.Error.Code);
        Assert.True(closed.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.Equal(RoutineExperimentStatus.Completed, replay.Value!.Status);
        Assert.Single(repo.Learnings);
        Assert.True(repo.Learnings[0].IsPrivate);
    }

    [Fact]
    public void Migration_and_ui_expose_routine_experiment_contract()
    {
        var migration = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrations", "106_routine_experiments.sql"));
        var script = File.ReadAllText(RepositoryRootLocator.PathTo("database", "script_completo.sql"));
        var view = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "Habits", "Detail.cshtml"));

        Assert.Contains("routine_experiments", migration);
        Assert.Contains("routine_experiment_learnings", migration);
        Assert.Contains("ck_routine_experiment_periods", migration);
        Assert.Contains("-- BEGIN include database/migrations/106_routine_experiments.sql", script);
        Assert.Contains("asp-action=\"CreateExperiment\"", view);
        Assert.Contains("não cria check-ins", view);
    }

    private RoutineExperimentDraftCommand Command(
        string name = "Teste de horário",
        DateOnly? baselineStart = null,
        DateOnly? baselineEnd = null,
        DateOnly? testStart = null,
        DateOnly? testEnd = null) =>
        new(clientId, userId, habitId, name, "Sempre esqueço no fim do dia",
            RoutineExperimentAdjustmentType.ChangeTime, "Testar pela manhã",
            testStart ?? new(2026, 10, 8), testEnd ?? new(2026, 10, 14),
            baselineStart ?? new(2026, 10, 1), baselineEnd ?? new(2026, 10, 7),
            RoutineExperimentIndicator.CompletionRate, "Observação privada");

    private RoutineExperimentService Service(ExperimentRepositoryFake repo, CompletionRepositoryFake? completions = null)
    {
        var habit = new Habit(habitId, userId, "Meditar", "#10B981", "Bem-estar", false, null,
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), DateTime.UtcNow,
            FrequencyType: HabitFrequencyType.Daily, ClientId: clientId, EstimatedTimeMinutes: 10,
            MinimumVersionName: "2 minutos", MinimumVersionQuantity: 2, TargetQuantity: 10, TargetUnit: "min");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Progress:DefaultTimeZone"] = "America/Sao_Paulo" }).Build();
        return new(repo, new HabitRepositoryFake(habit), new WeekDayRepositoryFake(), completions ?? new CompletionRepositoryFake(),
            new ExceptionRepositoryFake(), new HabitOccurrenceService(), new UserTimeZoneService(config, TimeProvider.System, NullLogger<UserTimeZoneService>.Instance), TimeProvider.System);
    }

    private sealed class ExperimentRepositoryFake : IRoutineExperimentRepository
    {
        public List<RoutineExperiment> Experiments { get; } = [];
        public List<RoutineExperimentEvent> Events { get; } = [];
        public List<RoutineExperimentLearning> Learnings { get; } = [];
        public Task<RoutineExperiment?> GetAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(Experiments.SingleOrDefault(x => x.ClientId == clientId && x.UserId == userId && x.Id == id));
        public Task<IReadOnlyList<RoutineExperiment>> ListByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RoutineExperiment>>(Experiments.Where(x => x.ClientId == clientId && x.UserId == userId && x.HabitId == habitId).ToList());
        public Task<IReadOnlyList<RoutineExperiment>> ListActiveForHabitAsync(Guid clientId, Guid userId, Guid habitId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RoutineExperiment>>(Experiments.Where(x => x.ClientId == clientId && x.UserId == userId && x.HabitId == habitId && x.Status == RoutineExperimentStatus.Active && x.TestStart <= to && x.TestEnd >= from).ToList());
        public Task CreateAsync(RoutineExperiment experiment, CancellationToken ct = default) { Experiments.Add(experiment); return Task.CompletedTask; }
        public Task<bool> UpdateAsync(RoutineExperiment experiment, int expectedVersion, CancellationToken ct = default)
        {
            var index = Experiments.FindIndex(x => x.Id == experiment.Id && x.Version == expectedVersion);
            if (index < 0) return Task.FromResult(false);
            Experiments[index] = experiment;
            return Task.FromResult(true);
        }
        public Task AddEventAsync(RoutineExperimentEvent value, CancellationToken ct = default) { Events.Add(value); return Task.CompletedTask; }
        public Task AddLearningAsync(RoutineExperimentLearning value, CancellationToken ct = default) { Learnings.Add(value); return Task.CompletedTask; }
        public Task<IReadOnlyList<RoutineExperimentLearning>> ListLearningsByHabitAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RoutineExperimentLearning>>(Learnings.Where(x => x.ClientId == clientId && x.UserId == userId && x.HabitId == habitId).ToList());
    }

    private sealed class HabitRepositoryFake(Habit habit) : IHabitRepository
    {
        public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(1);
        public Task<IReadOnlyList<Habit>> ListByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Habit>>([habit]);
        public Task<IReadOnlyList<Habit>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Habit>>([habit]);
        public Task<IReadOnlyList<Habit>> ListActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Habit>>([habit]);
        public Task<Habit?> FindByIdempotencyKeyAsync(Guid clientId, Guid userId, Guid idempotencyKey, CancellationToken ct = default) => Task.FromResult<Habit?>(null);
        public Task<Habit?> FindActiveBySourceTemplateAsync(Guid clientId, Guid userId, Guid templateId, bool includeVariations, CancellationToken ct = default) => Task.FromResult<Habit?>(null);
        public Task<int> CountActiveAsync(Guid clientId, Guid userId, CancellationToken ct = default) => Task.FromResult(1);
        public Task<Habit?> GetAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default) => Task.FromResult(habit.ClientId == clientId && habit.UserId == userId && habit.Id == habitId ? habit : null);
        public Task CreateAsync(Habit value, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> UpdateAsync(Guid clientId, Guid userId, Habit value, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class WeekDayRepositoryFake : IHabitWeekDayRepository
    {
        public Task<IReadOnlyList<HabitWeekDay>> ListByHabitAsync(Guid habitId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabitWeekDay>>([]);
        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<HabitWeekDay>>> ListByHabitsAsync(IEnumerable<Guid> habitIds, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<HabitWeekDay>>>(new Dictionary<Guid, IReadOnlyList<HabitWeekDay>>());
        public Task ReplaceAsync(Guid habitId, IReadOnlyCollection<int> days, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CompletionRepositoryFake : IHabitCompletionRepository
    {
        public List<HabitCompletion> Rows { get; } = [];
        public Task<IReadOnlyList<HabitCompletion>> ListByUserAsync(Guid userId, DateOnly? from = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabitCompletion>>(Rows.Where(x => x.UserId == userId && (!from.HasValue || x.CompletedDate >= from)).ToList());
        public Task<IReadOnlyList<HabitCompletion>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabitCompletion>>(Rows.Where(x => x.UserId == userId && x.CompletedDate >= from && x.CompletedDate <= to).ToList());
        public Task<CompletionMutationResult> AddIfMissingAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, Guid completionId, CancellationToken ct = default) => Task.FromResult(new CompletionMutationResult(completionId, true, false, true, localDate));
        public Task<CompletionMutationResult> DeleteIfExistsAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, CancellationToken ct = default) => Task.FromResult(new CompletionMutationResult(null, false, false, false, localDate));
        public Task AddAsync(HabitCompletion completion, CancellationToken ct = default) { Rows.Add(completion); return Task.CompletedTask; }
        public Task DeleteAsync(Guid habitId, Guid userId, DateOnly date, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ExceptionRepositoryFake : IHabitScheduleExceptionRepository
    {
        public Task<IReadOnlyList<HabitScheduleException>> ListAsync(Guid clientId, Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabitScheduleException>>([]);
        public Task<HabitScheduleException?> GetAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, CancellationToken ct = default) => Task.FromResult<HabitScheduleException?>(null);
        public Task<ScheduleExceptionMutationResult> UpsertAsync(HabitScheduleException exception, int expectedVersion, CancellationToken ct = default) => Task.FromResult(new ScheduleExceptionMutationResult(ScheduleExceptionMutationStatus.Created, 1));
        public Task<ScheduleExceptionMutationResult> DeleteAsync(Guid clientId, Guid userId, Guid habitId, DateOnly localDate, int expectedVersion, CancellationToken ct = default) => Task.FromResult(new ScheduleExceptionMutationResult(ScheduleExceptionMutationStatus.Updated, 0));
    }
}
