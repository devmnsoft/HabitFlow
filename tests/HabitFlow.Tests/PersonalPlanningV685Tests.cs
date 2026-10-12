using Xunit;

namespace HabitFlow.Tests;

public sealed class PersonalPlanningV685Tests
{
    [Fact]
    public void Migration_creates_priority_next_step_and_monthly_review_contracts()
    {
        var migration = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "database", "migrations", "108_personal_planning_monthly_review.sql"));
        Assert.Contains("habitflow.personal_priorities", migration);
        Assert.Contains("habitflow.dismissed_next_steps", migration);
        Assert.Contains("habitflow.monthly_reviews", migration);
        Assert.Contains("ux_dismissed_next_steps_day", migration);
        Assert.Contains("status in ('Draft','Completed')", migration);
        Assert.Contains("ux_monthly_reviews_idempotency", migration);

        var script = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "database", "script_completo.sql"));
        Assert.Contains("BEGIN include database/migrations/108_personal_planning_monthly_review.sql", script);
    }

    [Fact]
    public void Services_keep_metrics_deterministic_and_changes_confirmable()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Application", "Services", "PersonalPlanningService.cs"));
        Assert.Contains("DailyRoutinePlannerService", source);
        Assert.Contains("ListDismissedSuggestionKeysAsync", source);
        Assert.Contains("Dispensar", File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "Views", "MyDay", "Partials", "_DailyCommandCenter.cshtml")));
        Assert.Contains("IA externa não foi chamada", source);
        Assert.Contains("Escolher uma proposta registra a decisão", File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "Views", "MonthlyReview", "Show.cshtml")));
        Assert.Contains("A aplicação em hábitos, horários ou experimentos continua manual", File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "Views", "MonthlyReview", "Show.cshtml")));
    }

    [Fact]
    public void Routes_are_private_and_navigable()
    {
        var nav = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "Services", "NavigationService.cs"));
        Assert.Contains("/priorities", nav);
        Assert.Contains("/monthly-review", nav);

        var middleware = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "Middleware", "AccountStatusMiddleware.cs"));
        Assert.Contains("\"/priorities\"", middleware);
        Assert.Contains("\"/monthly-review\"", middleware);

        var sw = File.ReadAllText(Path.Combine(RepositoryRootLocator.Find(), "src", "HabitFlow.Web", "wwwroot", "service-worker.js"));
        Assert.Contains("priorities", sw);
        Assert.Contains("monthly-review", sw);
    }
}
