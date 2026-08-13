using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitRepositoryContractTests
{
    [Fact]
    public void Create_supplies_non_null_start_date_parameter()
    {
        var source = ContractSource.Read("src/HabitFlow.Infrastructure/Repositories/HabitRepository.cs");
        Assert.Contains("start_date", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@StartDate", source, StringComparison.Ordinal);
        Assert.Contains("h.StartDate", source, StringComparison.Ordinal);
    }
}

public sealed class UserGoalRepositoryContractTests
{
    [Fact]
    public void Linked_habits_use_the_canonical_projection_and_tenant_scope()
    {
        var source = ContractSource.Read("src/HabitFlow.Infrastructure/Repositories/UserGoalRepository.cs");
        Assert.Contains("HabitSql.AliasedColumns", source, StringComparison.Ordinal);
        Assert.DoesNotContain("select h.*", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("g.client_id=@c and g.user_id=@u", source, StringComparison.Ordinal);
    }
}

public sealed class CrudSmokeRepositoryTests
{
    [Fact]
    public void Critical_mutations_are_tenant_and_user_scoped()
    {
        var habits = ContractSource.Read("src/HabitFlow.Infrastructure/Repositories/HabitRepository.cs");
        var goals = ContractSource.Read("src/HabitFlow.Infrastructure/Repositories/UserGoalRepository.cs");
        Assert.Contains("where id=@Id and client_id=@clientId and user_id=@userId", habits, StringComparison.Ordinal);
        Assert.Contains("g.client_id=@c and g.user_id=@u", goals, StringComparison.Ordinal);
    }
}

public sealed class DapperProjectionTests
{
    [Fact]
    public void Domain_habit_queries_never_use_star_or_legacy_visibility()
    {
        var repositories = Directory.GetFiles(Path.Combine(ContractSource.Root, "src/HabitFlow.Infrastructure/Repositories"), "*.cs")
            .Select(File.ReadAllText);
        var habitQueries = string.Join('\n', repositories.Where(x => x.Contains("QueryAsync<Habit>", StringComparison.Ordinal) || x.Contains("QuerySingleOrDefaultAsync<Habit>", StringComparison.Ordinal)));
        Assert.DoesNotContain("select *", habitQueries, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("select h.*", habitQueries, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("h.visibility", habitQueries, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ContractSource
{
    internal static string Root => RepositoryRootLocator.Root;
    internal static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));
}
