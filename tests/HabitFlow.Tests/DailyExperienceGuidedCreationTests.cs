using Xunit;

namespace HabitFlow.Tests;

public sealed class DailyExperienceGuidedCreationTests
{
    [Fact]
    public void Habit_Editor_Exposes_Guided_Adaptive_Fields()
    {
        var editor = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "Habits", "Partials", "_HabitEditor.cshtml"));
        var schedule = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "Habits", "Partials", "_HabitScheduleEditor.cshtml"));
        var page = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "Habits", "Editor.cshtml"));

        Assert.Contains("data-editor-step", editor);
        Assert.Contains("data-tracking-mode", editor);
        Assert.Contains("name=\"TargetQuantity\"", editor);
        Assert.Contains("name=\"TargetUnit\"", editor);
        Assert.Contains("name=\"MinimumVersionName\"", editor);
        Assert.Contains("name=\"MinimumVersionQuantity\"", editor);
        Assert.Contains("name=\"StartDate\"", schedule);
        Assert.Contains("name=\"EndDate\"", schedule);
        Assert.Contains("name=\"RetroactiveAdjustmentDays\"", schedule);
        Assert.Contains("data-preview-tracking", page);
        Assert.Contains("data-preview-period", page);
    }

    [Fact]
    public void Habit_Editor_Javascript_Protects_Daily_Experience()
    {
        var script = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "wwwroot", "js", "habits-v4.js"));

        Assert.Contains("setQuantityEnabled", script);
        Assert.Contains("missingQuantityPair", script);
        Assert.Contains("invalidDateRange", script);
        Assert.Contains("data-preview-minimum", script);
        Assert.Contains("dataset.minimumQuantity", script);
    }

    [Fact]
    public void Habit_Editor_Styles_Are_Accessible_And_Responsive()
    {
        var css = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "wwwroot", "css", "habits-v4.css"));

        Assert.Contains(".hf-editor-step", css);
        Assert.Contains(".hf-choice-grid input:focus-visible+span", css);
        Assert.Contains("@media(max-width:767px)", css);
        Assert.Contains(".hf-adaptive-fields.is-disabled", css);
    }

    [Fact]
    public void Navigation_Access_Matrix_Covers_Daily_Routes_And_Guards()
    {
        var doc = File.ReadAllText(RepositoryRootLocator.PathTo("docs", "NAVIGATION_ACCESS_MATRIX.md"));

        Assert.Contains("/habits/create", doc);
        Assert.Contains("/my-day", doc);
        Assert.Contains("/progress/calendar", doc);
        Assert.Contains("basic_reports", doc);
        Assert.Contains("Client.Users.Manage", doc);
        Assert.Contains("Controllers and use cases remain responsible", doc);
    }

    [Fact]
    public void Controller_Maps_Adaptive_Errors_To_Editor_Fields()
    {
        var controller = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Controllers", "HabitsController.cs"));

        Assert.Contains("\"habit.validity_range\" => \"EndDate\"", controller);
        Assert.Contains("\"habit.retroactive_window\" => \"RetroactiveAdjustmentDays\"", controller);
        Assert.Contains("\"habit.quantity_unit\" or \"habit.unit_invalid\" => \"TargetUnit\"", controller);
        Assert.Contains("\"habit.minimum_quantity_positive\" or \"habit.minimum_quantity_too_high\" => \"MinimumVersionQuantity\"", controller);
    }
}
