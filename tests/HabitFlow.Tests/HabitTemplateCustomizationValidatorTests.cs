using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitTemplateCustomizationValidatorTests
{
    private readonly HabitTemplateCustomizationValidator _validator = new();
    private static readonly DateOnly Today = new(2026, 8, 1);

    [Fact]
    public void Accepts_complete_custom_weekly_configuration()
    {
        var result = _validator.Validate(Command() with
        {
            FrequencyType = HabitFrequencyType.CustomWeekly,
            TargetPerWeek = 3,
            SelectedDays = [1, 3, 5],
            PreferredTime = new TimeOnly(7, 30),
            CreateGoal = true,
            GoalTitle = "Ler com constância",
            GoalTargetType = GoalTargetType.HabitCompletions,
            GoalTargetValue = 12
        }, Today);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("red")]
    [InlineData("")]
    public void Rejects_colors_outside_the_server_palette(string color)
    {
        var result = _validator.Validate(Command() with { Color = color }, Today);
        Assert.Equal("template.color_invalid", result.Error.Code);
    }

    [Fact]
    public void Rejects_duplicate_days_and_mismatched_target()
    {
        var result = _validator.Validate(Command() with
        {
            FrequencyType = HabitFrequencyType.CustomWeekly,
            TargetPerWeek = 3,
            SelectedDays = [1, 1, 5]
        }, Today);
        Assert.Equal("template.days_invalid", result.Error.Code);
    }

    private static CreateHabitFromTemplateCommand Command() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leitura matinal", HabitFrequencyType.Daily,
        7, [], null, "#2563EB", "Leitura", null, Today, null, false, null, null, null,
        false, null, null, Guid.NewGuid(), "test-correlation");
}
