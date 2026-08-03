namespace HabitFlow.Web.Services;

/// <summary>Stable names for the local HabitFlow icon sprite.</summary>
public static class IconCodes
{
    public const string Home = "home";
    public const string MyDay = "my-day";
    public const string Habits = "habits";
    public const string Goals = "goals";
    public const string Progress = "progress";
    public const string Calendar = "calendar";
    public const string Library = "library";
    public const string Collections = "collections";
    public const string WeeklyReview = "weekly-review";
    public const string Reminders = "reminders";
    public const string Notifications = "notifications";
    public const string Profile = "profile";
    public const string Search = "search";
    public const string Add = "add";
    public const string Edit = "edit";
    public const string Delete = "delete";
    public const string Close = "close";
    public const string Check = "check";
    public const string Information = "information";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Success = "success";

    public static IReadOnlySet<string> All { get; } = typeof(IconCodes)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);
}
