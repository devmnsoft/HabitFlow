using HabitFlow.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
[Route("admin")]
public sealed class AdminSaaSController : Controller
{
    private static readonly IReadOnlyDictionary<string, (string Title, string Description, string Permission)> Pages =
        new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenants"] = ("Tenants", "Configuração, plano, retenção e status dos espaços de trabalho.", AdminPermissions.DashboardRead),
            ["roles"] = ("Papéis e permissões", "RBAC centralizado com escopo obrigatório por tenant.", AdminPermissions.UsersUpdateRole),
            ["audit"] = ("Auditoria", "Eventos sensíveis sanitizados e rastreáveis por correlation ID.", AdminPermissions.AuditRead),
            ["feature-flags"] = ("Feature flags", "Liberação gradual por ambiente, tenant e plano aplicada no backend.", AdminPermissions.FeatureFlagsManage),
            ["privacy"] = ("Privacidade e LGPD", "Exportação, anonimização, consentimentos e retenção legal.", AdminPermissions.PrivacyManage)
        };

    [HttpGet("{section:regex(^(tenants|roles|audit|feature-flags|privacy)$)}")]
    public IActionResult Section(string section)
    {
        var page = Pages[section];
        return View("~/Views/Admin/SaaSSection.cshtml", new AdminSaaSPage(section, page.Title, page.Description, page.Permission));
    }
}

public sealed record AdminSaaSPage(string Section, string Title, string Description, string RequiredPermission);
