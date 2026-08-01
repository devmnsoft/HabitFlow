using HabitFlow.Domain;
using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed record HabitTemplateDetailsViewModel(HabitTemplate Template, bool IsFavorite);

public sealed class CustomizeHabitTemplateViewModel
{
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateDescription { get; set; } = string.Empty;
    public string? Benefit { get; set; }
    public string? FirstAction { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public HabitDifficulty Difficulty { get; set; }
    public string MinimumPlanCode { get; set; } = PlanCodes.Free;
    public string Name { get; set; } = string.Empty;
    public HabitFrequencyType FrequencyType { get; set; } = HabitFrequencyType.Daily;
    public int? TargetPerWeek { get; set; }
    public int[] SelectedDays { get; set; } = [];
    public TimeOnly? PreferredTime { get; set; }
    public string Color { get; set; } = "#2563EB";
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public Guid? ExistingGoalId { get; set; }
    public bool CreateGoal { get; set; }
    public string? GoalTitle { get; set; }
    public GoalTargetType? GoalTargetType { get; set; }
    public decimal? GoalTargetValue { get; set; }
    public bool AllowVariation { get; set; }
    public IReadOnlyList<UserGoal> AvailableGoals { get; set; } = [];
    public IReadOnlyList<string> AvailableCategories { get; set; } = [];
    public IReadOnlyCollection<string> AllowedColors { get; set; } = [];
    public TemplatePlanUsage? PlanUsage { get; set; }
    public bool PlanLimitReached { get; set; }
    public string? ReturnUrl { get; set; }
    public bool IsOnboarding { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
}
