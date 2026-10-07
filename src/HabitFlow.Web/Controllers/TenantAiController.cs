using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin,TenantAdmin,TenantOwner,SuperAdmin")]
[Route("admin/ai")]
public sealed class TenantAiController(AiAdminService admin, PlanEntitlementService entitlements) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        try
        {
            ViewBag.Limit = await admin.EffectiveLimitAsync(clientId, ct);
            ViewBag.Usage = (await admin.ConsumptionAsync(clientId, ct)).FirstOrDefault();
            ViewBag.Included = await entitlements.GetBooleanFeatureAsync(userId, PlanFeatureCodes.AiAssistant, ct);
            return View(await admin.RecentAsync(clientId, ct));
        }
        catch
        {
            ViewBag.Limit = 0;
            ViewBag.Usage = null;
            ViewBag.Included = false;
            ViewBag.Unavailable = true;
            return View(Array.Empty<AiUsageEventRow>());
        }
    }
}
