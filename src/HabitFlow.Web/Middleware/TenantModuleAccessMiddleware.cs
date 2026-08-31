using System.Security.Claims;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class TenantModuleAccessMiddleware(RequestDelegate next)
{
    private static readonly IReadOnlyDictionary<string, string> Routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/habits"] = TenantModules.Habits, ["/goals"] = TenantModules.Goals,
        ["/routines"] = TenantModules.Routines, ["/calendar"] = TenantModules.Calendar,
        ["/notifications"] = TenantModules.Notifications, ["/analytics"] = TenantModules.Analytics,
        ["/gamification"] = TenantModules.Gamification, ["/assistant"] = TenantModules.Assistant,
        ["/integrations"] = TenantModules.Integrations, ["/billing"] = TenantModules.Billing,
        ["/support"] = TenantModules.Support
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || context.User.IsInRole(nameof(UserRole.SuperAdmin))) { await next(context); return; }
        var path = context.Request.Path.Value ?? string.Empty;
        var module = Routes.FirstOrDefault(x => path.StartsWith(x.Key, StringComparison.OrdinalIgnoreCase)).Value;
        if (module is null) { await next(context); return; }

        var status = context.User.FindFirstValue("tenant_status");
        if (string.Equals(status, nameof(TenantStatus.CommerciallyBlocked), StringComparison.OrdinalIgnoreCase))
        {
            await Deny(context, "Conta temporariamente bloqueada", "Fale com comercial@mnsoft.com.br — MNSOFT CNPJ 18.160.057/0001-13."); return;
        }
        var moduleClaims = context.User.FindAll("tenant_module").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (moduleClaims.Count > 0 && !moduleClaims.Contains(module))
        {
            await Deny(context, "Módulo indisponível", "Este módulo não está habilitado para sua organização. Solicite acesso ao administrador."); return;
        }
        await next(context);
    }

    private static async Task Deny(HttpContext context, string title, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { status = 403, title, detail });
    }
}
