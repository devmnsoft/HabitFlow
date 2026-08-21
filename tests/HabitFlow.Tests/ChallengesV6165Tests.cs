using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class ChallengesV6165Tests
{
    private static readonly string Root=RepositoryRootLocator.Root;

    [Theory]
    [InlineData(0,0)] [InlineData(1,14)] [InlineData(7,100)] [InlineData(8,100)]
    public void Progress_is_bounded_and_explainable(int completed,int expected)
    {
        var today=new DateOnly(2026,8,20); var challenge=new UserChallenge(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"7 dias","passo",7,today,today.AddDays(6),UserChallengeStatus.Active,DateTime.UtcNow,DateTime.UtcNow,null,completed);
        Assert.Equal(expected,challenge.ProgressPercent);
    }

    [Fact]
    public void Challenge_catalog_only_markets_implemented_backend_features()
    {
        var registry=new PlanFeatureImplementationRegistry();
        foreach(var code in new[]{"challenge_7_days","challenge_30_days","challenge_90_days"})
        { var feature=registry.Find(code); Assert.NotNull(feature); Assert.Equal(PlanFeatureImplementationStatus.Implemented,feature!.Status); Assert.True(feature.IsMarketable); }
    }

    [Fact]
    public void Migration_and_repository_enforce_tenant_ownership()
    {
        var migration=File.ReadAllText(Path.Combine(Root,"database","migrations","069_v6165_intelligent_onboarding_challenges.sql"));
        var repository=File.ReadAllText(Path.Combine(Root,"src","HabitFlow.Infrastructure","Repositories","UserChallengeRepository.cs"));
        Assert.Contains("client_id",migration,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.client_id=@clientId and c.user_id=@userId",repository,StringComparison.Ordinal);
        Assert.Contains("ux_user_challenges_active_habit",migration,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Archived_or_paused_habits_cannot_receive_new_completions()
    {
        var repository=File.ReadAllText(Path.Combine(Root,"src","HabitFlow.Infrastructure","Repositories","HabitCompletionRepository.cs"));
        Assert.Contains("h.is_archived = false and h.is_paused = false",repository,StringComparison.OrdinalIgnoreCase);
    }
}
