using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class PlansController(PlanService plans, IConfiguration config, IWebHostEnvironment env) : Controller
{
    [HttpGet("plans")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await plans.GetPublicPlansAsync(ct);
        ViewBag.PaymentMode = config["Payment:Mode"] ?? "Sandbox";
        ViewBag.ShowSandbox = !env.IsProduction() && string.Equals(ViewBag.PaymentMode, "Sandbox", StringComparison.OrdinalIgnoreCase);
        if (result.IsFailure) { TempData["Error"] = result.Error.Message; return View(Array.Empty<HabitFlow.Domain.Plan>()); }
        return View(result.Value);
    }
}
