using System.Xml.Linq;

namespace HabitFlow.Tests;

public sealed class ArchitectureDependencyTests
{
    [Fact]
    public void Projects_follow_clean_architecture_dependency_direction()
    {
        var root = FindRoot();
        AssertReferences(root, "HabitFlow.Domain", []);
        AssertReferences(root, "HabitFlow.Application", ["HabitFlow.Domain", "HabitFlow.Shared"]);
        AssertReferences(root, "HabitFlow.Infrastructure", ["HabitFlow.Application", "HabitFlow.Domain", "HabitFlow.Shared"]);
        AssertReferences(root, "HabitFlow.Web", ["HabitFlow.Application", "HabitFlow.Infrastructure", "HabitFlow.Domain", "HabitFlow.Shared"]);
    }

    private static void AssertReferences(string root, string project, string[] allowed)
    {
        var path = Path.Combine(root, "src", project, $"{project}.csproj");
        var references = XDocument.Load(path).Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension((string?)x.Attribute("Include"))).ToArray();
        Assert.All(references, reference => Assert.Contains(reference, allowed));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Solution root not found.");
    }
}
