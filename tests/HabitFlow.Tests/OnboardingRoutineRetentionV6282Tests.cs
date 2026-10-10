using Xunit;

namespace HabitFlow.Tests;

public sealed class OnboardingRoutineRetentionV6282Tests
{
    [Fact]
    public void Onboarding_Start_Does_Not_Reset_Completed_Progress()
    {
        var repository = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Infrastructure", "Repositories", "UserOnboardingRepositories.cs"));

        Assert.Contains("where habitflow.user_onboarding_progress.completed_at is null", repository);
    }

    [Fact]
    public void Onboarding_Controller_Completes_And_Does_Not_Repeat_Terminal_Flow()
    {
        var controller = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Controllers", "OnboardingController.cs"));
        var journey = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Application", "Services", "OnboardingJourneyService.cs"));

        Assert.Contains("progress?.Status == OnboardingStatus.Completed", controller);
        Assert.Contains("RedirectToAction(\"Index\", \"MyDay\")", controller);
        Assert.Contains("personalJourney.CompleteAsync", controller);
        Assert.Contains("CurrentStep = OnboardingStep.Completed", journey);
        Assert.Contains("await drafts.DeleteAsync(clientId, userId, ct)", journey);
    }

    [Fact]
    public void Onboarding_Template_Skip_Preserves_Version()
    {
        var templates = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Web", "Views", "Onboarding", "Templates.cshtml"));

        Assert.Contains("OnboardingVersion", templates);
        Assert.Contains("name=\"version\"", templates);
    }

    [Fact]
    public void Daily_And_Weekly_Planners_Use_Adaptive_Habit_Schedule_Fields()
    {
        var daily = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Application", "Services", "DailyRoutinePlannerService.cs"));
        var weekly = File.ReadAllText(RepositoryRootLocator.PathTo("src", "HabitFlow.Application", "Services", "WeeklyReviewService.cs"));

        Assert.Contains("StartDate = h.StartDate", daily);
        Assert.Contains("EndDate = h.EndDate", daily);
        Assert.Contains("IsPaused = h.IsPaused", daily);
        Assert.Contains("TargetQuantity = h.TargetQuantity", daily);
        Assert.Contains("StartDate = habit.StartDate", weekly);
        Assert.Contains("EndDate = habit.EndDate", weekly);
        Assert.Contains("IsPaused = habit.IsPaused", weekly);
        Assert.Contains("TargetQuantity = habit.TargetQuantity", weekly);
    }

    [Fact]
    public void Audit_Document_Classifies_Requested_Journeys()
    {
        var doc = File.ReadAllText(RepositoryRootLocator.PathTo("docs", "ONBOARDING_ROUTINE_RETENTION_AUDIT.md"));

        Assert.Contains("Navegacao por perfil e plano", doc);
        Assert.Contains("Criacao guiada de habitos", doc);
        Assert.Contains("Trial e cobranca", doc);
        Assert.Contains("Pendencias externas", doc);
    }
}
