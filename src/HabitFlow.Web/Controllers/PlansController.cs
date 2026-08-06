using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class PlansController(HabitFlow.Web.Services.PlanLandingPageService landing, IConfiguration config, IWebHostEnvironment env, ILogger<PlansController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        HabitFlow.Web.Models.PlanLandingPageViewModel result;
        try
        {
            result = await landing.BuildAsync(ct);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Não foi possível carregar o catálogo público de planos");
            result = new([], [], [], [], [], new("Comece gratuitamente", "O catálogo está sendo atualizado.", "Começar grátis", "/register"));
            TempData["Warning"] = "Os detalhes dos planos estão temporariamente indisponíveis. Você ainda pode começar gratuitamente.";
        }

        ViewBag.PaymentMode = config["Payment:Mode"] ?? "Sandbox";
        ViewBag.ShowSandbox = !env.IsProduction() && string.Equals(ViewBag.PaymentMode, "Sandbox", StringComparison.OrdinalIgnoreCase);
        return View(result);
    }
}
