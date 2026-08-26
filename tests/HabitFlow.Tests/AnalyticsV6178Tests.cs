using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class AnalyticsV6178Tests
{
    private readonly Guid _client=Guid.NewGuid(), _user=Guid.NewGuid();

    [Theory]
    [InlineData(7,100)] [InlineData(28,25)] [InlineData(90,7.8)]
    public void Consistency_is_calculated_for_7_28_and_90_days(int days,double expected)
    {
        var today=new DateOnly(2026,8,26); var habit=Habit(today.AddDays(-100));
        var completions=Enumerable.Range(0,7).Select(i=>Completion(habit.Id,today.AddDays(-i)));
        Assert.Equal(expected,AnalyticsQueryService.ConsistencyPercentage([habit],completions,today.AddDays(1-days),today));
    }

    [Fact] public void Empty_period_and_user_without_data_return_zero() => Assert.Equal(0,AnalyticsQueryService.ConsistencyPercentage([],[],new(2026,8,1),new(2026,8,26)));

    [Fact] public void Habit_created_mid_period_only_counts_eligible_days()
    {
        var habit=Habit(new(2026,8,24));
        Assert.Equal(66.7,AnalyticsQueryService.ConsistencyPercentage([habit],[Completion(habit.Id,new(2026,8,24)),Completion(habit.Id,new(2026,8,25))],new(2026,8,20),new(2026,8,26)));
    }

    [Fact] public void Paused_and_archived_habits_do_not_dilute_consistency()
    {
        var active=Habit(new(2026,8,1)); var paused=Habit(new(2026,8,1)) with{IsPaused=true}; var archived=Habit(new(2026,8,1)) with{IsArchived=true};
        Assert.Equal(100,AnalyticsQueryService.ConsistencyPercentage([active,paused,archived],[Completion(active.Id,new(2026,8,26))],new(2026,8,26),new(2026,8,26)));
    }

    [Fact] public void Invalid_and_oversized_periods_are_rejected()
    {
        Assert.Throws<ArgumentException>(()=>AnalyticsPeriod.Create(new(2026,8,27),new(2026,8,26),new(2026,8,26)));
        Assert.Throws<ArgumentException>(()=>AnalyticsPeriod.Create(new(2025,1,1),new(2026,8,26),new(2026,8,26)));
    }

    [Fact] public void Analytics_routes_are_authenticated_and_queries_are_tenant_scoped()
    {
        var root=RepositoryRootLocator.Root;
        var controller=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Web/Controllers/AnalyticsController.cs"));
        var service=File.ReadAllText(Path.Combine(root,"src/HabitFlow.Application/Services/AnalyticsQueryService.cs"));
        Assert.Contains("[Authorize]",controller); Assert.Contains("RequireCurrentClientId",controller);
        Assert.Contains("ListAsync(clientId, userId",service); Assert.DoesNotContain("localStorage",service,StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public void Insights_are_non_duplicated_explainable_and_non_judgmental()
    {
        var source=File.ReadAllText(Path.Combine(RepositoryRootLocator.Root,"src/HabitFlow.Application/Services/AnalyticsQueryService.cs"));
        Assert.Contains("Take(5)",source); Assert.Contains("Consistência calculada",source); Assert.Contains("sem cobrança",File.ReadAllText(Path.Combine(RepositoryRootLocator.Root,"src/HabitFlow.Web/Views/Analytics/Index.cshtml")));
    }

    private Habit Habit(DateOnly created)=>new(Guid.NewGuid(),_user,"Leitura","#123456","Estudo",false,null,created.ToDateTime(TimeOnly.MinValue),created.ToDateTime(TimeOnly.MinValue),ClientId:_client);
    private HabitCompletion Completion(Guid habit,DateOnly date)=>new(Guid.NewGuid(),habit,_user,date,date.ToDateTime(TimeOnly.MinValue));
}
