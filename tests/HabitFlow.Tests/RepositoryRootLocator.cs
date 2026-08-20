namespace HabitFlow.Tests;

internal static class RepositoryRootLocator
{
    private static readonly Lazy<string> Cached = new(Locate);
    public static string Root => Cached.Value;
    public static string PathTo(params string[] segments) => Path.Combine([Root, .. segments]);

    private static string Locate()
    {
        var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        foreach (var origin in new[] { workspace, AppContext.BaseDirectory }.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            for (var directory = new DirectoryInfo(origin); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln")) )
                    return directory.FullName;
        }
        throw new DirectoryNotFoundException("A raiz do HabitFlow não foi localizada a partir do diretório de testes.");
    }
}
