using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class PlansController(HabitFlow.Domain.IPlanCatalogRepository plans, IConfiguration config, IWebHostEnvironment env, ILogger<PlansController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        IReadOnlyList<HabitFlow.Domain.PublicPlan> result;
        try
        {
            result = await plans.GetPublicCatalogAsync(ct);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Não foi possível carregar o catálogo público de planos");
            result = [];
            TempData["Warning"] = "Os detalhes dos planos estão temporariamente indisponíveis. Você ainda pode começar gratuitamente.";
        }

        ViewBag.PaymentMode = config["Payment:Mode"] ?? "Sandbox";
        ViewBag.ShowSandbox = !env.IsProduction() && string.Equals(ViewBag.PaymentMode, "Sandbox", StringComparison.OrdinalIgnoreCase);
        return View(result);
    }
}
