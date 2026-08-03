namespace HabitFlow.Tests;

internal static class RepositoryRootLocator
{
    private static readonly Lazy<string> Cached = new(Locate);
    public static string Root => Cached.Value;
    public static string PathTo(params string[] segments) => Path.Combine([Root, .. segments]);

    private static string Locate()
    {
        foreach (var origin in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(origin); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln")) && Directory.Exists(Path.Combine(directory.FullName, ".git")))
                    return directory.FullName;
        }
        throw new DirectoryNotFoundException("A raiz do HabitFlow não foi localizada a partir do diretório de testes.");
    }
}
