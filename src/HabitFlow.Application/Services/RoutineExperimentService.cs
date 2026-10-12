using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record RoutineExperimentDraftCommand(
    Guid ClientId,
    Guid UserId,
    Guid HabitId,
    string Name,
    string Difficulty,
    RoutineExperimentAdjustmentType AdjustmentType,
    string ProposedAdjustment,
    DateOnly TestStart,
    DateOnly TestEnd,
    DateOnly BaselineStart,
    DateOnly BaselineEnd,
    RoutineExperimentIndicator PrimaryIndicator,
    string? Reflection);

public sealed record RoutineExperimentPeriodMetrics(DateOnly Start, DateOnly End, int Scheduled, int Completed, int Excused, int MinimumReached, int FullTargetReached, string Limitation);
public sealed record RoutineExperimentComparison(RoutineExperiment Experiment, RoutineExperimentPeriodMetrics Baseline, RoutineExperimentPeriodMetrics Test, string PrimaryLabel, decimal? BaselineValue, decimal? TestValue, IReadOnlyList<string> Limitations, string Summary);

public sealed class RoutineExperimentService(
    IRoutineExperimentRepository experiments,
    IHabitRepository habits,
    IHabitWeekDayRepository weekDays,
    IHabitCompletionRepository completions,
    IHabitScheduleExceptionRepository exceptions,
    HabitOccurrenceService occurrences,
    UserTimeZoneService timeZone,
    TimeProvider clock)
{
    public async Task<Result<RoutineExperiment>> CreateDraftAsync(RoutineExperimentDraftCommand command, CancellationToken ct = default)
    {
        var validation = Validate(command);
        if (validation.IsFailure) return Result<RoutineExperiment>.Failure(validation.Error.Code, validation.Error.Message);
        var habit = await habits.GetAsync(command.ClientId, command.UserId, command.HabitId, ct);
        if (habit is null) return Result<RoutineExperiment>.Failure("experiment.habit_not_found", "Hábito não encontrado.");
        var active = await experiments.ListActiveForHabitAsync(command.ClientId, command.UserId, command.HabitId, command.TestStart, command.TestEnd, ct);
        if (active.Any(x => x.AdjustmentType == command.AdjustmentType))
            return Result<RoutineExperiment>.Failure("experiment.adjustment_conflict", "Já existe experimento ativo para esse tipo de ajuste no período.");

        var now = clock.GetUtcNow();
        var experiment = new RoutineExperiment(Guid.NewGuid(), command.ClientId, command.UserId, command.HabitId,
            Clean(command.Name, 120), Clean(command.Difficulty, 500), command.AdjustmentType, Clean(command.ProposedAdjustment, 1000),
            command.TestStart, command.TestEnd, command.BaselineStart, command.BaselineEnd, command.PrimaryIndicator,
            RoutineExperimentStatus.Draft, CleanNullable(command.Reflection, 1000), Snapshot(habit), Proposed(command), RoutineExperimentDecision.None,
            1, now, now, null, null);
        await experiments.CreateAsync(experiment, ct);
        await experiments.AddEventAsync(Event(command.ClientId, command.UserId, experiment.Id, "DraftCreated", "Experimento criado em rascunho.", now), ct);
        return Result<RoutineExperiment>.Success(experiment);
    }

    public async Task<Result<RoutineExperiment>> StartAsync(Guid clientId, Guid userId, Guid id, int expectedVersion, CancellationToken ct = default)
    {
        var experiment = await experiments.GetAsync(clientId, userId, id, ct);
        if (experiment is null) return Result<RoutineExperiment>.Failure("experiment.not_found", "Experimento não encontrado.");
        if (experiment.Status != RoutineExperimentStatus.Draft) return Result<RoutineExperiment>.Failure("experiment.invalid_state", "Somente rascunhos podem ser iniciados.");
        var habit = await habits.GetAsync(clientId, userId, experiment.HabitId, ct);
        if (habit is null || habit.IsArchived || habit.IsPaused) return Result<RoutineExperiment>.Failure("experiment.habit_unavailable", "O hábito não está disponível para iniciar o teste.");
        var active = await experiments.ListActiveForHabitAsync(clientId, userId, experiment.HabitId, experiment.TestStart, experiment.TestEnd, ct);
        if (active.Any(x => x.Id != id && x.AdjustmentType == experiment.AdjustmentType))
            return Result<RoutineExperiment>.Failure("experiment.adjustment_conflict", "Outro teste já altera este mesmo campo no período.");

        var now = clock.GetUtcNow();
        var started = experiment with { Status = RoutineExperimentStatus.Active, StartedAt = now, UpdatedAt = now, Version = experiment.Version + 1 };
        if (!await experiments.UpdateAsync(started, expectedVersion, ct))
            return Result<RoutineExperiment>.Failure("experiment.conflict", "O experimento mudou em outra sessão. Atualize e tente novamente.");
        await experiments.AddEventAsync(Event(clientId, userId, id, "Started", "Experimento iniciado após confirmação do usuário.", now), ct);
        return Result<RoutineExperiment>.Success(started);
    }

    public async Task<Result<RoutineExperimentComparison>> CompareAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
    {
        var experiment = await experiments.GetAsync(clientId, userId, id, ct);
        if (experiment is null) return Result<RoutineExperimentComparison>.Failure("experiment.not_found", "Experimento não encontrado.");
        var habit = await habits.GetAsync(clientId, userId, experiment.HabitId, ct);
        if (habit is null) return Result<RoutineExperimentComparison>.Failure("experiment.habit_not_found", "Hábito não encontrado.");
        var baseline = await MetricsAsync(clientId, userId, habit, experiment.BaselineStart, experiment.BaselineEnd, ct);
        var test = await MetricsAsync(clientId, userId, habit, experiment.TestStart, experiment.TestEnd, ct);
        var (label, before, after) = Primary(experiment.PrimaryIndicator, baseline, test);
        var limitations = Limitations(baseline, test).ToList();
        var summary = BuildSummary(label, before, after, limitations);
        return Result<RoutineExperimentComparison>.Success(new(experiment, baseline, test, label, before, after, limitations, summary));
    }

    public async Task<Result<RoutineExperiment>> CloseAsync(Guid clientId, Guid userId, Guid id, RoutineExperimentDecision decision, RoutineExperimentLearningOutcome? outcome, string? observation, int expectedVersion, CancellationToken ct = default)
    {
        var experiment = await experiments.GetAsync(clientId, userId, id, ct);
        if (experiment is null) return Result<RoutineExperiment>.Failure("experiment.not_found", "Experimento não encontrado.");
        if (experiment.Status is RoutineExperimentStatus.Completed or RoutineExperimentStatus.Canceled)
            return Result<RoutineExperiment>.Success(experiment);
        if (decision == RoutineExperimentDecision.RestorePrevious)
            return Result<RoutineExperiment>.Failure("experiment.restore_requires_manual_review", "A restauração exige revisão manual para não sobrescrever mudanças posteriores.");
        var now = clock.GetUtcNow();
        var closed = experiment with { Status = RoutineExperimentStatus.Completed, Decision = decision, ClosedAt = now, UpdatedAt = now, Version = experiment.Version + 1 };
        if (!await experiments.UpdateAsync(closed, expectedVersion, ct))
            return Result<RoutineExperiment>.Failure("experiment.conflict", "O experimento mudou em outra sessão. Atualize e tente novamente.");
        await experiments.AddEventAsync(Event(clientId, userId, id, "Closed", $"Experimento encerrado com decisão {decision}.", now), ct);
        if (outcome.HasValue && !string.IsNullOrWhiteSpace(observation))
            await experiments.AddLearningAsync(new(Guid.NewGuid(), clientId, userId, experiment.HabitId, id, outcome.Value, Clean(observation, 2000), true, now), ct);
        return Result<RoutineExperiment>.Success(closed);
    }

    private async Task<RoutineExperimentPeriodMetrics> MetricsAsync(Guid clientId, Guid userId, Habit habit, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var days = (await weekDays.ListByHabitAsync(habit.Id, ct)).Select(x => x.DayOfWeek).ToHashSet();
        var planned = (await occurrences.ListScheduledForPeriodAsync([ToProgressRow(habit)], new Dictionary<Guid, IReadOnlySet<int>> { [habit.Id] = days }, start, end, timeZone.Resolve())).ToList();
        var exceptionList = await exceptions.ListAsync(clientId, userId, start, end, ct);
        var excused = exceptionList.Count(e => e.HabitId == habit.Id && e.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved && e.LocalDate >= start && e.LocalDate <= end);
        planned.RemoveAll(x => exceptionList.Any(e => e.HabitId == habit.Id && e.LocalDate == x.Date && e.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved));
        var completed = await completions.ListAsync(clientId, userId, start, end, ct);
        var relevant = completed.Where(x => x.HabitId == habit.Id && planned.Any(p => p.Date == x.CompletedDate)).ToList();
        var minimum = relevant.Count(x => x.CompletionMode.Equals("Minimum", StringComparison.OrdinalIgnoreCase));
        var full = relevant.Count - minimum;
        var limitation = planned.Count < 3 ? "Amostra pequena: use como sinal, não como conclusão." : "";
        return new(start, end, planned.Count, relevant.Count, excused, minimum, full, limitation);
    }

    private static (string Label, decimal? Baseline, decimal? Test) Primary(RoutineExperimentIndicator indicator, RoutineExperimentPeriodMetrics baseline, RoutineExperimentPeriodMetrics test) => indicator switch
    {
        RoutineExperimentIndicator.CompletionRate => ("taxa de registro", Rate(baseline.Completed, baseline.Scheduled), Rate(test.Completed, test.Scheduled)),
        RoutineExperimentIndicator.MinimumVersionCount => ("versões mínimas", baseline.MinimumReached, test.MinimumReached),
        RoutineExperimentIndicator.FullTargetCount => ("alvos completos", baseline.FullTargetReached, test.FullTargetReached),
        _ => ("registros realizados", baseline.Completed, test.Completed)
    };

    private static IEnumerable<string> Limitations(RoutineExperimentPeriodMetrics baseline, RoutineExperimentPeriodMetrics test)
    {
        if (baseline.Scheduled == 0) yield return "Período de referência sem ocorrências previstas.";
        if (test.Scheduled == 0) yield return "Período de teste sem ocorrências previstas.";
        if (baseline.Scheduled != test.Scheduled) yield return "Os períodos têm quantidades diferentes de ocorrências previstas.";
        if (!string.IsNullOrWhiteSpace(baseline.Limitation)) yield return "Referência com amostra pequena.";
        if (!string.IsNullOrWhiteSpace(test.Limitation)) yield return "Teste com amostra pequena.";
        if (baseline.Excused > 0 || test.Excused > 0) yield return "Dias dispensados ou movidos foram excluídos da comparação.";
    }

    private static string BuildSummary(string label, decimal? before, decimal? after, IReadOnlyList<string> limitations)
    {
        if (!before.HasValue || !after.HasValue) return $"Não há dados suficientes para comparar {label}.";
        var direction = after > before ? "maior" : after < before ? "menor" : "igual";
        var caution = limitations.Count > 0 ? " Leia as limitações antes de decidir." : "";
        return $"No teste, {label} ficou {direction} que na referência. Isso é comparação descritiva, não prova de causa.{caution}";
    }

    private static Result Validate(RoutineExperimentDraftCommand command)
    {
        if (command.ClientId == Guid.Empty || command.UserId == Guid.Empty || command.HabitId == Guid.Empty) return Result.Failure("experiment.identity", "Conta, pessoa e hábito são obrigatórios.");
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 120) return Result.Failure("experiment.name", "Informe um nome curto para o teste.");
        if (string.IsNullOrWhiteSpace(command.Difficulty)) return Result.Failure("experiment.difficulty", "Descreva a dificuldade observada.");
        if (string.IsNullOrWhiteSpace(command.ProposedAdjustment)) return Result.Failure("experiment.adjustment", "Descreva o ajuste proposto.");
        if (command.TestEnd < command.TestStart || command.BaselineEnd < command.BaselineStart) return Result.Failure("experiment.period", "Os períodos devem ter início e fim válidos.");
        if (command.TestStart <= command.BaselineEnd) return Result.Failure("experiment.period_overlap", "O teste deve começar após o período de referência.");
        return Result.Success();
    }

    private static decimal? Rate(int completed, int scheduled) => scheduled == 0 ? null : Math.Round(completed * 100m / scheduled, 1);
    private static string Snapshot(Habit h) => JsonSerializer.Serialize(new { h.FrequencyType, h.TargetPerWeek, h.ReminderTime, h.EstimatedTimeMinutes, h.MinimumVersionName, h.MinimumVersionQuantity });
    private static string Proposed(RoutineExperimentDraftCommand c) => JsonSerializer.Serialize(new { type = c.AdjustmentType.ToString(), proposal = c.ProposedAdjustment });
    private static RoutineExperimentEvent Event(Guid clientId, Guid userId, Guid experimentId, string type, string message, DateTimeOffset now) => new(Guid.NewGuid(), clientId, userId, experimentId, type, message, now);
    private static string Clean(string value, int max) => value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
    private static string? CleanNullable(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Clean(value, max);
    private static ProgressHabitRow ToProgressRow(Habit habit) => new()
    {
        Id = habit.Id, Name = habit.Name, Category = habit.Category, CreatedAt = habit.StartDate?.ToDateTime(TimeOnly.MinValue) ?? habit.CreatedAt,
        StartDate = habit.StartDate, EndDate = habit.EndDate, ArchivedAt = habit.ArchivedAt, IsArchived = habit.IsArchived,
        FrequencyTypeCode = habit.FrequencyType.ToString(), ReminderTime = habit.ReminderTime, IsPaused = habit.IsPaused,
        PausedAt = habit.PausedAt, TargetQuantity = habit.TargetQuantity, TargetUnit = habit.TargetUnit,
        MinimumVersionName = habit.MinimumVersionName, MinimumVersionQuantity = habit.MinimumVersionQuantity,
        RetroactiveAdjustmentDays = habit.RetroactiveAdjustmentDays
    };
}
