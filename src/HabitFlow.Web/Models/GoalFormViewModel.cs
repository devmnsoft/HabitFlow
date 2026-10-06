using System.ComponentModel.DataAnnotations;
using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed class GoalFormViewModel
{
    public Guid? GoalId { get; init; }

    [Required(ErrorMessage = "Informe onde você quer chegar."), StringLength(160, ErrorMessage = "Use no máximo 160 caracteres.")]
    public string Title { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Use no máximo 1.000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Escolha como medir a meta."), GoalTargetTypeCode]
    public string TargetType { get; set; } = "HabitCompletions";

    [Range(1, 100000, ErrorMessage = "A meta deve estar entre 1 e 100.000.")]
    public int TargetValue { get; set; } = 1;

    [Required(ErrorMessage = "Informe a data de início.")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [GoalEndDateAfterStart]
    public DateOnly? EndDate { get; set; }

    public static GoalFormViewModel Create() => new();
    public static GoalFormViewModel From(UserGoal goal) => new()
    {
        GoalId = goal.Id, Title = goal.Title, Description = goal.Description,
        TargetType = goal.TargetType, TargetValue = goal.TargetValue,
        StartDate = goal.StartDate, EndDate = goal.EndDate
    };
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class GoalTargetTypeCode : ValidationAttribute
{
    public GoalTargetTypeCode() : base("Escolha uma forma de medição válida.") { }
    private static readonly HashSet<string> Allowed =
        ["HabitCompletions", "ActiveDays", "StreakDays", "WeeklyCompletions", "Custom"];
    public override bool IsValid(object? value) => value is string s && Allowed.Contains(s);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class GoalEndDateAfterStart : ValidationAttribute
{
    public GoalEndDateAfterStart() : base("O prazo deve ser igual ou posterior à data de início.") { }
    public override bool RequiresValidationContext => true;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly end && validationContext?.ObjectInstance is GoalFormViewModel model && end < model.StartDate)
            return new ValidationResult("O prazo deve ser igual ou posterior à data de início.", new[] { nameof(GoalFormViewModel.EndDate) });
        return null;
    }
}
