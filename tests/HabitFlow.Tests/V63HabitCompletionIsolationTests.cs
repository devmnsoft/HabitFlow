namespace HabitFlow.Tests;

public sealed class V63HabitCompletionIsolationTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void CompletionRepository_scopes_deletion_to_current_user()
    {
        var source = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Infrastructure/Repositories/HabitCompletionRepository.cs"));

        Assert.Contains("habit_id=@habitId and user_id=@userId and completed_date=@date", source);
    }

    [Fact]
    public void HabitService_checks_ownership_before_mutating_completion()
    {
        var source = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Application/Services/HabitService.cs"));

        Assert.Equal(2, source.Split("!habit.BelongsTo(user.Id)").Length - 1);
        Assert.Contains("DeleteAsync(habitId, user.Id", source);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
