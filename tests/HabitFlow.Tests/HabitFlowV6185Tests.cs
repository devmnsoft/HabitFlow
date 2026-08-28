using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitFlowV6185Tests
{
    [Fact]
    public void Habit_list_query_is_bounded_and_rejects_unknown_filters()
    {
        var query = HabitListPolicy.Normalize(new HabitListQuery(
            Search: $"  {new string('a', 150)}  ", Category: $" {new string('b', 100)} ",
            Status: "technical-status", Sort: "unsafe-sort", Page: -4, PageSize: 500));

        Assert.Equal(120, query.Search!.Length);
        Assert.Equal(80, query.Category!.Length);
        Assert.Equal("active", query.Status);
        Assert.Equal("newest", query.Sort);
        Assert.Equal(1, query.Page);
        Assert.Equal(48, query.PageSize);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 4, 50)]
    [InlineData(7, 4, 100)]
    public void Consistency_is_safe_and_never_exceeds_one_hundred(int completed, int scheduled, decimal expected)
        => Assert.Equal(expected, HabitListPolicy.Consistency(completed, scheduled));

    [Fact]
    public void Sensitive_habit_operations_use_canonical_audit_event_names()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRootLocator.Root,
            "src/HabitFlow.Application/Services/HabitCoreServices.cs"));

        Assert.Contains("\"habit.created\"", source);
        Assert.Contains("\"habit.updated\"", source);
        Assert.Contains("\"plan.limit_reached\"", source);
        Assert.DoesNotContain("\"habit_created\"", source);
        Assert.DoesNotContain("\"habit_updated\"", source);
    }
}
