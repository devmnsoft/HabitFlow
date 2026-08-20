using Xunit;

namespace HabitFlow.Tests;

public sealed class MvpJourneyV6164Tests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(
        RepositoryRootLocator.Root, relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void Reminder_candidate_uses_explicit_persistence_projection()
    {
        var repository = Read("src/HabitFlow.Infrastructure/Repositories/ReminderDispatchRepository.cs");
        Assert.Contains("QueryAsync<ReminderDispatchCandidateRow>", repository);
        Assert.Contains("DateTime.SpecifyKind(ScheduledFor, DateTimeKind.Utc)", repository);
        Assert.DoesNotContain("QueryAsync<ReminderDispatchCandidate>(", repository);
    }

    [Fact]
    public void My_day_inline_time_form_carries_antiforgery_token()
    {
        var view = Read("src/HabitFlow.Web/Views/MyDay/Partials/_RoutineActionMenu.cshtml");
        var timeForm = view[view.IndexOf("/time\"", StringComparison.Ordinal)..];
        timeForm = timeForm[..timeForm.IndexOf("</form>", StringComparison.Ordinal)];
        Assert.Contains("@Html.AntiForgeryToken()", timeForm);
    }

    [Fact]
    public void Habit_creation_uses_catalog_limit_and_supports_unlimited_value()
    {
        var service = Read("src/HabitFlow.Application/Services/HabitCoreServices.cs");
        Assert.Contains("PlanFeatureCodes.ActiveHabitsLimit", service);
        Assert.Contains("limit is >= 0 && count >= limit", service);
        Assert.Contains("Veja os planos", service);
    }

    [Fact]
    public void Completion_and_reminder_storage_are_idempotent_and_tenant_scoped()
    {
        var completions = Read("src/HabitFlow.Infrastructure/Repositories/HabitCompletionRepository.cs");
        var reminders = Read("src/HabitFlow.Infrastructure/Repositories/ReminderDispatchRepository.cs");
        Assert.Contains("on conflict (habit_id, completed_date) do nothing", completions);
        Assert.Contains("h.client_id = @clientId", completions);
        Assert.Contains("on conflict(client_id,user_id,deduplication_key)", reminders);
        Assert.Contains("and client_id=@ClientId and user_id=@UserId and habit_id=@HabitId", reminders);
    }
}
