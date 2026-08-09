using System.Security.Claims;

namespace HabitFlow.Web.Services;

public sealed class HeaderActionService
{
    public bool HasBillingAccess(ClaimsPrincipal user) => user.IsInRole("SuperAdmin") || user.HasClaim("permission", "Client.Billing.View");
    public string ResolvePlanName(ClaimsPrincipal user) => user.FindFirst("plan_name")?.Value ?? user.FindFirst("plan")?.Value ?? "Gratuito";
}
