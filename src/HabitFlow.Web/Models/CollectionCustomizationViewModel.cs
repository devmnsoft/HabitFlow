using HabitFlow.Application;
using HabitFlow.Domain;
using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Web.Models;

public sealed class CollectionCustomizationViewModel
{
    public HabitTemplateCollectionDetails? Details { get; set; }
    public Guid CollectionId { get; set; }
    public List<CollectionCustomizationItemViewModel> Items { get; set; } = [];
    public Guid? ExistingGoalId { get; set; }
    public bool CreateGoal { get; set; }
    [StringLength(140)] public string? GoalTitle { get; set; }
    public GoalTargetType? GoalTargetType { get; set; }
    [Range(1,1_000_000)] public decimal? GoalTargetValue { get; set; }
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public IReadOnlyList<UserGoal> Goals { get; set; } = [];
    public TemplatePlanUsage PlanUsage { get; set; } = new(0,null,int.MaxValue);
}

public sealed class CollectionCustomizationItemViewModel
{
    public Guid TemplateId { get; set; }
    public bool Included { get; set; }
    [Required,StringLength(120,MinimumLength=2)] public string Name { get; set; } = "";
    public HabitFrequencyType FrequencyType { get; set; }
    [Range(1,7)] public int? TargetPerWeek { get; set; }
    public int[] SelectedDays { get; set; }=[];
    public TimeOnly? PreferredTime { get; set; }
    [Required] public string Color { get; set; }="#2563EB";
    [StringLength(80)] public string? Category { get; set; }
    public DateOnly StartDate { get; set; }
}
