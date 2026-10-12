using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed record PrioritiesViewModel(IReadOnlyList<PersonalPriorityDetails> Priorities, IReadOnlyList<UserGoal> ActiveGoals, DateOnly EffectiveFrom);

public sealed class PriorityFormModel
{
    public Guid[] GoalIds { get; set; } = [];
    public int? WeeklyAvailableMinutes { get; set; }
}

public sealed class MonthlyReviewFormModel
{
    public string? Reflection { get; set; }
    public string? SelectedNextStep { get; set; }
    public string? ConfirmedPlanCode { get; set; }
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    public int Version { get; set; }
}

public sealed record MonthlyReviewViewModel(MonthlyReviewResult Result, MonthlyReviewFormModel Form);
