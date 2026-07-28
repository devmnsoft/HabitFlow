namespace HabitFlow.Web.Services;

public static class NavigationIconCatalog
{
    public static IReadOnlySet<string> Names { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "home", "demo", "library", "premium", "help", "dashboard", "habit", "target",
        "progress", "report", "profile", "users", "invite", "billing", "privacy",
        "organization", "calendar", "warning", "settings"
    };

    public static bool Contains(string icon) => Names.Contains(icon);
}
