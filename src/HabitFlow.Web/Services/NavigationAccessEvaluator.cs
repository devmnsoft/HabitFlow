using System.Security.Claims;
using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public interface INavigationAccessEvaluator
{
    Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken);
}

public sealed class NavigationAccessEvaluator(FeatureAccessService features) : INavigationAccessEvaluator
{
    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken)
    {
        if (!HasPermission(user, permission)) return false;
        if (feature is null) return true;
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        return (await features.CheckFeatureAsync(userId, feature, cancellationToken)).Allowed;
    }

    private static bool HasPermission(ClaimsPrincipal user, string? permission) =>
        permission is null || user.IsInRole("SuperAdmin") ||
        user.Claims.Any(claim => claim.Type == "permission" &&
            (claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("Platform.FullAccess", StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("platform.view", StringComparison.OrdinalIgnoreCase)));
}
