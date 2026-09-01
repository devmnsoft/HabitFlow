using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HealthyGamificationV6173Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;

    [Fact]
    public void AchievementEvaluator_unlocks_each_applicable_stable_code_once()
    {
        var result = new AchievementEvaluator().Evaluate(new(30, 7, true));
        Assert.Equal(result.Distinct().Count(), result.Count);
        Assert.Contains("first_completion", result);
        Assert.Contains("consistency_3", result);
        Assert.Contains("consistency_7", result);
        Assert.Contains("total_30", result);
        Assert.Contains("weekly_goal_completed", result);
    }

    [Fact]
    public void Migration_is_additive_idempotent_and_tenant_scoped()
    {
        var sql = File.ReadAllText(Path.Combine(Root,"database/migrations/076_v6173_healthy_gamification.sql"));
        foreach(var table in new[]{"weekly_goals","weekly_goal_habits","achievement_definitions","user_achievements","user_missions","streak_freezes","gamification_events"})
            Assert.Contains($"create table if not exists {table}",sql,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique(client_id,user_id,achievement_code)",sql,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique(client_id,user_id,habit_id,frozen_date)",sql,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on conflict",sql,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Freeze_is_not_a_habit_completion_and_premium_is_server_enforced()
    {
        var repo=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Infrastructure/Repositories/GamificationRepository.cs"));
        var service=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Application/Services/GamificationServices.cs"));
        Assert.DoesNotContain("insert into habitflow.habit_completions",repo,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CheckFeatureAsync(userId,\"streak_freeze\"",service);
        Assert.Contains("plan.feature_locked",service);
    }

    [Fact]
    public void Progress_experience_is_personal_calm_and_responsive()
    {
        var view=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/Views/Gamification/Progress.cshtml"));
        var css=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/wwwroot/css/gamification.css"));
        Assert.Contains("Sem comparação com outras pessoas",view);
        Assert.Contains("Hoje conta. Amanhã continua.",view);
        Assert.Contains("@media(max-width:767px)",css);
        Assert.DoesNotContain("Você falhou",view,StringComparison.OrdinalIgnoreCase);
    }
}
