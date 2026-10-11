using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ProgramParticipationV6281Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;

    [Fact]
    public void Program_join_requires_explicit_habit_selection()
    {
        var domain = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Domain", "Entities", "HabitJourney.cs"));
        var service = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Application", "Services", "HabitJourneyAutomationServices.cs"));
        var controller = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Web", "Controllers", "JourneysController.cs"));

        Assert.Contains("JoinHabitJourneyHabitSelection", domain);
        Assert.Contains("command.HabitSelections.Count == 0", service);
        Assert.Contains("selectedStepIds", controller);
    }

    [Fact]
    public void Program_membership_links_created_habits_without_treating_join_as_completion()
    {
        var migration = File.ReadAllText(Path.Combine(Root, "database", "migrations", "104_program_participation_personalization.sql"));
        var repository = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Infrastructure", "Repositories", "HabitJourneyRepository.cs"));
        var view = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Web", "Views", "Journeys", "Details.cshtml"));

        Assert.Contains("habit_journey_member_habits", migration);
        Assert.Contains("status IN ('Draft','Published','Paused','Ended','Archived')", migration);
        Assert.Contains("habit_completions c", repository);
        Assert.Contains("Conclusao do programa", view);
        Assert.Contains("Aderir nao conclui", view);
    }

    [Fact]
    public void Program_catalog_blocks_private_programs_from_public_queries()
    {
        var migration = File.ReadAllText(Path.Combine(Root, "database", "migrations", "104_program_participation_personalization.sql"));
        var repository = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Infrastructure", "Repositories", "HabitJourneyRepository.cs"));
        var completeScript = File.ReadAllText(Path.Combine(Root, "database", "script_completo.sql"));

        Assert.Contains("is_private boolean", migration);
        Assert.Contains("is_private = false or client_id = @clientId", repository);
        Assert.Contains("-- START include database/migrations/104_program_participation_personalization.sql", completeScript);
    }

    [Fact]
    public void Challenge_rules_still_prevent_duplicate_active_challenges()
    {
        var migration = File.ReadAllText(Path.Combine(Root, "database", "migrations", "069_v6165_intelligent_onboarding_challenges.sql"));
        var service = File.ReadAllText(Path.Combine(Root, "src", "HabitFlow.Application", "Services", "UserChallengeService.cs"));

        Assert.Contains("ux_user_challenges_active_habit", migration);
        Assert.Contains("GetActiveAsync", service);
        Assert.Contains("challenge.duplicate", service);
    }
}
