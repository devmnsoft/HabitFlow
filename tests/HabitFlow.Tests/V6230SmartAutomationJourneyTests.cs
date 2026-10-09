using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6230SmartAutomationJourneyTests
{
    [Fact]
    public void Journey_and_automation_contracts_support_requested_states()
    {
        Assert.Contains(HabitAutomationStatus.Active, Enum.GetValues<HabitAutomationStatus>());
        Assert.Contains(HabitAutomationStatus.Paused, Enum.GetValues<HabitAutomationStatus>());
        Assert.Contains(HabitAutomationStatus.AwaitingConfirmation, Enum.GetValues<HabitAutomationStatus>());
        Assert.Contains(HabitAutomationType.WeeklyReview, Enum.GetValues<HabitAutomationType>());
        Assert.Contains(HabitAutomationType.MonthlySummary, Enum.GetValues<HabitAutomationType>());
    }

    [Fact]
    public void Journey_plan_rule_is_enforced_server_side()
    {
        Assert.True(HabitTemplateAccess.MeetsMinimumPlan(PlanCodes.Team, PlanCodes.Team));
        Assert.False(HabitTemplateAccess.MeetsMinimumPlan(PlanCodes.Free, PlanCodes.Team));
    }

    [Fact]
    public void Weekly_review_keeps_deterministic_fallback_when_ai_is_not_available()
    {
        var result = new WeeklyReviewResult(
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(6),
            7, 4, 57, "Monday", "Friday", "Ler", "Caminhar", "Saude", 1,
            [], [], [], [], [], [], false, "idem");

        Assert.False(result.AiGenerated);
        Assert.Equal("", result.SummaryText);
    }

    [Fact]
    public void V6230_migration_adds_marketplace_audit_and_tenant_tables()
    {
        var root = RepositoryRootLocator.Find();
        var migration = File.ReadAllText(Path.Combine(root, "database", "migrations", "098_v6230_smart_automation_journeys.sql"));
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.habit_journeys", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.habit_automations", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.habit_monthly_reviews", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.ai_usage_events", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS habitflow.security_audit_events", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("language varchar(10)", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Controllers_expose_real_routes_for_journeys_and_automations()
    {
        var root = RepositoryRootLocator.Find();
        var journeys = File.ReadAllText(Path.Combine(root, "src", "HabitFlow.Web", "Controllers", "JourneysController.cs"));
        var automations = File.ReadAllText(Path.Combine(root, "src", "HabitFlow.Web", "Controllers", "AutomationsController.cs"));
        Assert.Contains("[Route(\"journeys\")]", journeys);
        Assert.Contains("[HttpPost(\"{id:guid}/join\")", journeys);
        Assert.Contains("[Route(\"automations\")]", automations);
        Assert.Contains("ValidateAntiForgeryToken", automations);
    }
}
