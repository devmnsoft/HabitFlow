namespace HabitFlow.Web.Services;

public sealed class ActiveNavigationMatcher
{
    public bool Matches(string? currentPath, string? targetUrl)
    {
        var path = Normalize(currentPath);
        var target = Normalize(targetUrl);
        if (target == "/") return path == target;
        return path.Equals(target, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(target + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        var path = (value ?? "/").Split('?', '#')[0].TrimEnd('/');
        return string.IsNullOrEmpty(path) ? "/" : path;
    }
}
