using System.ComponentModel.DataAnnotations;
using HabitFlow.Web.Models;

namespace HabitFlow.Tests;

public sealed class GlobalFormValidationV6175Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));
    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    [Fact]
    public void Goal_form_rejects_required_length_range_code_and_date_errors()
    {
        var model = new GoalFormViewModel { Title = new string('x', 161), TargetType = "OtherTenantCode", TargetValue = 0, StartDate = new(2026, 8, 25), EndDate = new(2026, 8, 24) };
        var errors = Validate(model);
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.Title)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.TargetType)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.TargetValue)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.EndDate)));
    }

    [Fact]
    public void Goal_form_accepts_a_valid_accessible_submission()
    {
        var model = new GoalFormViewModel { Title = "Caminhar com constância", Description = "Começar com calma", TargetType = "ActiveDays", TargetValue = 20, StartDate = new(2026, 8, 25), EndDate = new(2026, 9, 25) };
        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Mutation_forms_have_global_antiforgery_and_accessible_ux()
    {
        var registration = Read("src/HabitFlow.Web/Configuration/DependencyInjection.cs");
        var script = Read("src/HabitFlow.Web/wwwroot/js/form-validation.js");
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", registration);
        Assert.Contains("aria-invalid", script);
        Assert.Contains("aria-required", script);
        Assert.Contains("data-hf-client-summary", script);
        Assert.Contains("form.dataset.submitting", script);
    }

    [Fact]
    public void Goal_identity_is_route_derived_and_habit_is_a_real_selection()
    {
        var editor = Read("src/HabitFlow.Web/Views/Goals/Partials/_GoalEditor.cshtml");
        var linked = Read("src/HabitFlow.Web/Views/Goals/Partials/_GoalLinkedHabits.cshtml");
        Assert.DoesNotContain("name=\"GoalId\"", editor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("asp-for=\"GoalId\"", editor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<select", linked);
        Assert.Contains("habit.Name", linked);
    }
}
