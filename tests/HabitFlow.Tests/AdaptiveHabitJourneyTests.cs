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
        Assert.Equal("habit.minimum_quantity_too_high", service.ValidatePlanning(new(null, null, 10, "pages", "Ler uma pagina", 12, 7)).Error.Code);
        Assert.Equal("habit.retroactive_window", service.ValidatePlanning(new(null, null, null, null, null, null, 40)).Error.Code);
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
        Assert.False(partial.IsCompleted);
        Assert.True(complete.IsCompleted);
        Assert.Equal("pages", complete.Unit);
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

