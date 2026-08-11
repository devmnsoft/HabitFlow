using System.Security.Claims;

namespace HabitFlow.Web.Models;

public sealed record HeaderNavigationItem(string Code, string Label, string Description, string Icon, string Url, bool IsActive, bool IsVisible = true);
public sealed record HeaderNavigationGroup(string Label, IReadOnlyList<HeaderNavigationItem> Items);
public sealed record HeaderNavigationViewModel(IReadOnlyList<HeaderNavigationItem> Primary, IReadOnlyList<HeaderNavigationGroup> Secondary, IReadOnlyList<HeaderNavigationItem> MobileBottom);
public sealed record HeaderQuickActionViewModel(string Label, string Description, string Url, string Icon, bool IsPlanGated = false);
public sealed record NotificationBellViewModel(bool IsEnabled, int? UnreadCount = null);
public sealed record HeaderDensityViewModel(string Name, bool IsCompactBrand, bool ShowsBottomNavigation);
public sealed record HeaderDebugViewModel(string Route, string Context, bool IsAuthenticated, string Density);
public sealed record AppHeaderViewModel(
    bool IsAuthenticated,
    NavigationContext Context,
    string UserName,
    string UserEmail,
    string Initial,
    string CurrentPath,
    string PlanName,
    bool HasPlatformAccess,
    bool HasBillingAccess,
    HeaderNavigationViewModel Navigation,
    IReadOnlyList<HeaderQuickActionViewModel> QuickActions,
    NotificationBellViewModel Notifications);

public sealed record PublicHeaderViewModel(AppHeaderViewModel Shared, HeaderDensityViewModel Density);
public sealed record AccountHeaderViewModel(AppHeaderViewModel Shared, HeaderDensityViewModel Density);
public sealed record HeaderDebugInfo(string Route, NavigationContext Context, bool IsAuthenticated, string Density);
