using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class PlansController(HabitFlow.Domain.IPlanCatalogRepository plans, IConfiguration config, IWebHostEnvironment env) : Controller
{
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await plans.GetPublicCatalogAsync(ct);
        ViewBag.PaymentMode = config["Payment:Mode"] ?? "Sandbox";
        ViewBag.ShowSandbox = !env.IsProduction() && string.Equals(ViewBag.PaymentMode, "Sandbox", StringComparison.OrdinalIgnoreCase);
        return View(result);
    }
}
