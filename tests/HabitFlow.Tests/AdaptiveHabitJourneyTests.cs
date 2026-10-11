using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AdaptiveHabitJourneyTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [Fact]
    public void Occurrence_Excludes_Dates_Before_Start_After_End_After_Archive_And_During_Current_Pause()
    {
        var habit = new ProgressHabitRow
        {
            Id = Guid.NewGuid(),
            Name = "Treinar",
            FrequencyTypeCode = HabitFrequencyType.Daily.ToString(),
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            StartDate = new DateOnly(2026, 1, 10),
            EndDate = new DateOnly(2026, 1, 20),
            IsPaused = true,
            PausedAt = new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc)
        };
        var service = new HabitOccurrenceService();

        Assert.False(service.IsScheduledForDate(habit, new HashSet<int>(), new DateOnly(2026, 1, 9), Zone));
        Assert.True(service.IsScheduledForDate(habit, new HashSet<int>(), new DateOnly(2026, 1, 15), Zone));
        Assert.False(service.IsScheduledForDate(habit, new HashSet<int>(), new DateOnly(2026, 1, 16), Zone));
        habit.IsPaused = false;
        Assert.False(service.IsScheduledForDate(habit, new HashSet<int>(), new DateOnly(2026, 1, 21), Zone));
    }

    [Fact]
    public void Planning_Rejects_Incompatible_Quantity_Unit_And_Invalid_Ranges()
    {
        var service = new AdaptiveHabitPlanningService();

        Assert.Equal("habit.validity_range", service.ValidatePlanning(new(new(2026, 2, 10), new(2026, 2, 1), null, null, null, null, 7)).Error.Code);
        Assert.Equal("habit.quantity_unit", service.ValidatePlanning(new(null, null, 10, null, null, null, 7)).Error.Code);
        Assert.Equal("habit.unit_invalid", service.ValidatePlanning(new(null, null, 10, "bananas", null, null, 7)).Error.Code);
        Assert.Equal("habit.minimum_quantity_required", service.ValidatePlanning(new(null, null, 10, "minutes", "Fazer o mínimo", null, 7)).Error.Code);
        Assert.Equal("habit.minimum_quantity_without_target", service.ValidatePlanning(new(null, null, null, null, "Fazer o mínimo", 1, 7)).Error.Code);
        Assert.Equal("habit.minimum_quantity_too_high", service.ValidatePlanning(new(null, null, 10, "pages", "Ler uma pagina", 12, 7)).Error.Code);
        Assert.Equal("habit.retroactive_window", service.ValidatePlanning(new(null, null, null, null, null, null, 40)).Error.Code);
    }

    [Theory]
    [InlineData("minutes", "min")]
    [InlineData("minutos", "min")]
    [InlineData("páginas", "pages")]
    [InlineData("lt", "liters")]
    public void Unit_Aliases_Are_Normalized_To_Canonical_Codes(string alias, string canonical)
    {
        var service = new AdaptiveHabitPlanningService();

        Assert.Equal(canonical, service.NormalizeUnit(alias));
    }

    [Fact]
    public void Quantity_Completion_Uses_Target_Without_Mixing_Units()
    {
        var service = new AdaptiveHabitPlanningService();
        var habit = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Ler", "#10B981", "Estudo", false, null,
            DateTime.UtcNow, DateTime.UtcNow, TargetQuantity: 20, TargetUnit: "pages");

        var partial = service.EvaluateCompletion(habit, 12);
        var complete = service.EvaluateCompletion(habit, 20);

        Assert.Equal(HabitCompletionMode.Quantity, partial.Mode);
        Assert.Equal(HabitCompletionState.Partial, partial.State);
        Assert.False(partial.IsCompleted);
        Assert.True(complete.IsCompleted);
        Assert.Equal(HabitCompletionState.TargetReached, complete.State);
        Assert.Equal("pages", complete.Unit);
    }

    [Fact]
    public void Minimum_Version_Is_Distinct_From_Full_Target()
    {
        var service = new AdaptiveHabitPlanningService();
        var habit = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Caminhar", "#10B981", "Saúde", false, null,
            DateTime.UtcNow, DateTime.UtcNow, TargetQuantity: 30, TargetUnit: "minutes",
            MinimumVersionName: "Caminhar 10 minutos", MinimumVersionQuantity: 10);

        var minimum = service.EvaluateCompletion(habit, 10, "min");

        Assert.Equal(HabitCompletionState.MinimumReached, minimum.State);
        Assert.True(minimum.MinimumReached);
        Assert.False(minimum.IsCompleted);
        Assert.Equal("min", minimum.Unit);
    }

    [Fact]
    public void Invalid_Quantity_Or_Unit_Does_Not_Complete_Habit()
    {
        var service = new AdaptiveHabitPlanningService();
        var quantityHabit = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Beber água", "#10B981", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow, TargetQuantity: 2, TargetUnit: "liters", MinimumVersionQuantity: 1);
        var binaryHabit = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Alongar", "#10B981", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow);

        Assert.Equal(HabitCompletionState.Invalid, service.EvaluateCompletion(quantityHabit, -1, "liters").State);
        Assert.Equal(HabitCompletionState.Invalid, service.EvaluateCompletion(quantityHabit, 1, "pages").State);
        Assert.Equal(HabitCompletionState.Invalid, service.EvaluateCompletion(binaryHabit, 1).State);
    }

    [Fact]
    public void Retroactive_Window_Is_Explicit_And_Does_Not_Allow_Future_Dates()
    {
        var service = new AdaptiveHabitPlanningService();
        var habit = new Habit(Guid.NewGuid(), Guid.NewGuid(), "Meditar", "#10B981", null, false, null,
            DateTime.UtcNow, DateTime.UtcNow, RetroactiveAdjustmentDays: 3);
        var today = new DateOnly(2026, 10, 10);

        Assert.True(service.CanAdjustDate(habit, today.AddDays(-3), today));
        Assert.False(service.CanAdjustDate(habit, today.AddDays(-4), today));
        Assert.False(service.CanAdjustDate(habit, today.AddDays(1), today));
    }

    [Fact]
    public void Daily_Routine_Proposal_Respects_Availability_And_Does_Not_Invent_Duration()
    {
        var planner = new DailyRoutinePlannerService(null!, null!, null!, null!, null!, null!, TimeProvider.System, null!);
        var habitA = Guid.NewGuid();
        var habitB = Guid.NewGuid();
        var habitC = Guid.NewGuid();
        var plan = new DailyRoutinePlan(new(2026, 10, 11),
        [
            new(habitA, "Meditar", "#10B981", null, null, null, new(8, 0), 10, HabitFrequencyType.Daily, DailyRoutineItemStatus.Available, 1, 0, "Concluir", 20, "min", "2 minutos", 2),
            new(habitB, "Ler", "#10B981", null, null, null, new(9, 0), 25, HabitFrequencyType.Daily, DailyRoutineItemStatus.Available, 2, 0, "Concluir", 20, "pages", "1 página", 1),
            new(habitC, "Respirar", "#10B981", null, null, null, null, 0, HabitFrequencyType.Daily, DailyRoutineItemStatus.Available, 3, 0, "Concluir")
        ], 3, 0, 0);

        var proposal = planner.Propose(plan, new(15, null, null, new HashSet<Guid> { habitB }, true, new HashSet<Guid>()));

        Assert.Contains(proposal.Items, x => x.HabitId == habitA && x.Included);
        Assert.Contains(proposal.Items, x => x.HabitId == habitB && !x.Included);
        Assert.Contains(proposal.Items, x => x.HabitId == habitC && x.Included && x.EstimatedMinutes is null);
        Assert.Contains(proposal.MissingData, x => x.Contains("Respirar"));
        Assert.Contains(proposal.Alternatives, x => x.Contains("Ler"));
    }

    [Fact]
    public void Migration_And_UseCase_Contain_Adaptive_Journey_Guards()
    {
        var migration = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrations", "103_adaptive_habit_journey.sql"));
        var completeScript = File.ReadAllText(RepositoryRootLocator.PathTo("database", "script_completo.sql"));
        var useCase = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Application", "Services", "HabitCompletionUseCases.cs"));

        Assert.Contains("target_quantity", migration);
        Assert.Contains("retroactive_adjustment_days", migration);
        Assert.Contains("recorded_quantity", migration);
        Assert.Contains("ck_habits_validity_range", migration);
        Assert.Contains("-- START include database/migrations/103_adaptive_habit_journey.sql", completeScript);
        Assert.Contains("habit.not_scheduled", useCase);
        Assert.Contains("habit.retroactive_window", useCase);
    }
}

