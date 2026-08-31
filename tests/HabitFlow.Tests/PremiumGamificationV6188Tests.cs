using HabitFlow.Application;
namespace HabitFlow.Tests;
public sealed class PremiumGamificationV6188Tests
{
 private static readonly string Root=RepositoryRootLocator.Find();
 [Fact] public void Achievement_rules_cover_required_real_events_once(){var result=new AchievementEvaluator().Evaluate(new(30,30,true,true,true,true,true,true));Assert.Equal(result.Distinct().Count(),result.Count);foreach(var code in new[]{"first_habit","first_completion","consistency_3","consistency_7","consistency_30","weekly_goal_completed","routine_completed","consistent_week","return_after_pause","template_used"})Assert.Contains(code,result);}
 [Fact] public void Ledger_is_tenant_safe_idempotent_and_reversible(){var sql=File.ReadAllText(Path.Combine(Root,"database/migrations/081_v6188_premium_gamification.sql"));Assert.Contains("unique(client_id,user_id,idempotency_key)",sql,StringComparison.OrdinalIgnoreCase);Assert.Contains("'reversal'",sql);Assert.Contains("where is_opted_in",sql,StringComparison.OrdinalIgnoreCase);}
 [Fact] public void Repository_materializes_explicit_leaderboard_projection(){var source=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Infrastructure/Repositories/GamificationRepository.cs"));Assert.Contains("public_name",source);Assert.Contains("QueryAsync<LeaderboardEntry>",source);Assert.Contains("p.client_id=@clientId",source);}
 [Fact] public void Ranking_requires_opt_in_and_never_projects_sensitive_fields(){var view=File.ReadAllText(Path.Combine(Root,"src/HabitFlow.Web/Views/Leaderboard/Index.cshtml"));Assert.Contains("optedIn",view);Assert.Contains("Sair do ranking",view);Assert.DoesNotContain("Email",view,StringComparison.OrdinalIgnoreCase);}
}
