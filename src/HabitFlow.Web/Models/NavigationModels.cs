namespace HabitFlow.Web.Models;

public enum NavigationContext { Public, Personal, Account, Platform }

public sealed record NavigationItem(
    string Code, string Label, string Description, string Icon, string Url,
    NavigationContext Context, string? RequiredPermission, string? RequiredFeature,
    int SortOrder, bool IsActive);
