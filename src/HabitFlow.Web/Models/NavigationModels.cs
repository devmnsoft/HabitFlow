namespace HabitFlow.Web.Models;

public enum NavigationContext { Public, Personal, Account, Platform }

public enum NavigationVariant
{
    PublicTop,
    PersonalTop,
    AccountSidebar,
    PlatformSidebar,
    MobileBottom,
    MobileDrawer
}

public sealed record NavigationItem(
    string Code, string Label, string Description, string Icon, string Url,
    NavigationContext Context, string? RequiredPermission, string? RequiredFeature,
    int SortOrder, bool IsEnabled, bool IsCurrent = false);

public sealed record NavigationViewModel(
    NavigationContext Context,
    NavigationVariant Variant,
    IReadOnlyList<NavigationItem> Items);
