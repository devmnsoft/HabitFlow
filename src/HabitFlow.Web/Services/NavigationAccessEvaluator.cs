using System.Security.Claims;
using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public interface INavigationAccessEvaluator
{
    Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken);
}

public sealed class NavigationAccessEvaluator(
    FeatureAccessService features,
    RequestPlanAccessContext requestPlan,
    ILogger<NavigationAccessEvaluator> logger) : INavigationAccessEvaluator
{
    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken)
    {
        if (!HasPermission(user, permission)) return false;
        if (feature is null) return true;
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        if (requestPlan.LoadFailed) return false;
        try
        {
            if (requestPlan.Features is null || requestPlan.UserId != userId)
            {
                requestPlan.UserId = userId;
                requestPlan.Features = await features.GetFeaturesForUserAsync(userId, cancellationToken);
            }

            return requestPlan.Features.TryGetValue(feature, out var value) && value.BoolValue == true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            requestPlan.LoadFailed = true;
            WebRuntimeDiagnostics.NavigationFeatureFailures.Add(1);
            logger.LogError(ex, "Falha segura ao avaliar feature {Feature} da navegação para usuário {UserIdMask}.", feature, $"{userId:N}"[..8] + "…");
            return false;
        }
    }

    private static bool HasPermission(ClaimsPrincipal user, string? permission) =>
        permission is null || user.IsInRole("SuperAdmin") ||
        user.Claims.Any(claim => claim.Type == "permission" &&
            (claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("Platform.FullAccess", StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("platform.view", StringComparison.OrdinalIgnoreCase)));
}
