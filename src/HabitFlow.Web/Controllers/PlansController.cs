using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public sealed class PlansController(HabitFlow.Web.Services.PlanLandingPageService landing, SubscriptionService subscriptions, IConfiguration config, IWebHostEnvironment env, ILogger<PlansController> logger) : Controller
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
            result = HabitFlow.Web.Services.PlanLandingPageService.BuildFallback();
            TempData["Warning"] = "Os detalhes dos planos estão temporariamente indisponíveis. Você ainda pode começar gratuitamente.";
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var subscription = await subscriptions.GetUserSubscriptionAsync(this.CurrentUserId(), ct);
            result = result with { Viewer = BuildViewer(subscription) };
        }
        logger.LogInformation("plans.page.viewed Authenticated={Authenticated} Plan={PlanCode}", result.Viewer.IsAuthenticated, result.Viewer.PlanCode);
        logger.LogInformation("plans.current_plan.rendered Plan={PlanCode} Status={Status}", result.Viewer.PlanCode, result.Viewer.Status);
        ViewBag.PaymentMode = config["Payment:Mode"] ?? "Sandbox";
        ViewBag.ShowSandbox = !env.IsProduction() && string.Equals(ViewBag.PaymentMode, "Sandbox", StringComparison.OrdinalIgnoreCase);
        return View(result);
    }

    private static HabitFlow.Web.Models.PlanViewerViewModel BuildViewer(HabitFlow.Domain.Subscription? subscription)
    {
        if (subscription is null) return new(true, HabitFlow.Domain.PlanCodes.Free, null, "Gratuito", "Você está no Gratuito e pode fazer upgrade quando quiser.", null, null, false);
        var end = subscription.CurrentPeriodEnd;
        var cycle = subscription.BillingCycle?.ToString();
        return subscription.Status switch
        {
            HabitFlow.Domain.SubscriptionStatus.Active when subscription.CanceledAt is not null => new(true, subscription.PlanCode, cycle, "Cancelamento agendado", $"Seu Premium continua ativo até {end:dd/MM/yyyy}.", end, null, true),
            HabitFlow.Domain.SubscriptionStatus.Active or HabitFlow.Domain.SubscriptionStatus.Trial => new(true, subscription.PlanCode, cycle, "Ativo", "Seu Premium está ativo.", end, end, true),
            HabitFlow.Domain.SubscriptionStatus.Pending => new(true, HabitFlow.Domain.PlanCodes.Free, cycle, "Pagamento pendente", "Aguardando confirmação do pagamento. Os recursos Premium ainda não foram liberados.", null, null, true),
            HabitFlow.Domain.SubscriptionStatus.PastDue => new(true, subscription.PlanCode, cycle, "Pagamento pendente", "Regularize o pagamento para evitar a perda dos recursos Premium.", end, null, true),
            HabitFlow.Domain.SubscriptionStatus.Canceled => new(true, HabitFlow.Domain.PlanCodes.Free, cycle, "Cancelado", "Sua assinatura terminou. Seus hábitos continuam guardados no Gratuito.", end, null, true),
            _ => new(true, HabitFlow.Domain.PlanCodes.Free, null, "Gratuito", "Você está no Gratuito e pode fazer upgrade quando quiser.", null, null, false)
        };
    }
}
