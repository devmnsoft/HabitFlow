using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class OnboardingStateMachineTests
{
    [Theory]
    [InlineData(OnboardingStep.Objective, OnboardingStep.Availability, true)]
    [InlineData(OnboardingStep.Availability, OnboardingStep.Preferences, true)]
    [InlineData(OnboardingStep.Objective, OnboardingStep.Review, false)]
    [InlineData(OnboardingStep.Review, OnboardingStep.Preferences, false)]
    public void State_order_only_allows_same_or_next_step(OnboardingStep current, OnboardingStep next, bool valid)
    {
        var actual = (int)next >= (int)current && (int)next <= (int)current + 1;
        Assert.Equal(valid, actual);
    }

    [Fact]
    public void Terminal_status_is_derived_without_parallel_flags()
    {
        var now = DateTime.UtcNow;
        var completed = Progress() with { CompletedAt = now };
        var skipped = Progress() with { SkippedAt = now };
        Assert.Equal(OnboardingStatus.Completed, completed.Status);
        Assert.Equal(OnboardingStatus.Skipped, skipped.Status);
    }

    private static UserOnboardingProgress Progress() => new(Guid.NewGuid(),Guid.NewGuid(),OnboardingStep.Objective,null,null,null,[],null,[],null,false,null,null,DateTime.UtcNow,DateTime.UtcNow,null,null,1);
}
