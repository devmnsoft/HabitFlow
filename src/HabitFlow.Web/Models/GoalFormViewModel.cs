using System.ComponentModel.DataAnnotations;
using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed class GoalFormViewModel : IValidatableObject
{
    private static readonly HashSet<string> TargetTypes =
        ["HabitCompletions", "ActiveDays", "StreakDays", "WeeklyCompletions", "Custom"];

    public Guid? GoalId { get; init; }

    [Required(ErrorMessage = "Informe onde você quer chegar."), StringLength(160, ErrorMessage = "Use no máximo 160 caracteres.")]
    public string Title { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Use no máximo 1.000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Escolha como medir a meta.")]
    public string TargetType { get; set; } = "HabitCompletions";

    [Range(1, 100000, ErrorMessage = "A meta deve estar entre 1 e 100.000.")]
    public int TargetValue { get; set; } = 1;

    [Required(ErrorMessage = "Informe a data de início.")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TargetTypes.Contains(TargetType))
            yield return new("Escolha uma forma de medição válida.", [nameof(TargetType)]);
        if (EndDate is { } end && end < StartDate)
            yield return new("O prazo deve ser igual ou posterior à data de início.", [nameof(EndDate)]);
    }

    public static GoalFormViewModel Create() => new();
    public static GoalFormViewModel From(UserGoal goal) => new()
    {
        GoalId = goal.Id, Title = goal.Title, Description = goal.Description,
        TargetType = goal.TargetType, TargetValue = goal.TargetValue,
        StartDate = goal.StartDate, EndDate = goal.EndDate
    };
}
