using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class AccountPlanController(PlanUsageService usage, IPlanCatalogRepository catalog, ILogger<AccountPlanController> logger) : Controller
{
    [HttpGet("account/plan")]
    public IActionResult Index() => RedirectToAction(nameof(Usage));

    [HttpGet("account/plan/usage")]
    public async Task<IActionResult> Usage(CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        try
        {
            var model = await usage.BuildAsync(clientId, userId, ct);
            return model is null ? Forbid() : View(model);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Não foi possível exibir o uso do plano");
            TempData["Warning"] = "Não foi possível atualizar os detalhes agora. Tente novamente em instantes.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet("account/plan/change/{planCode}")]
    public async Task<IActionResult> Change(string planCode, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var access = clientId == Guid.Empty ? null : await catalog.GetClientAccessAsync(clientId, ct);
        var plan = (await catalog.GetPublicCatalogAsync(ct)).FirstOrDefault(x => x.Code.Equals(planCode, StringComparison.OrdinalIgnoreCase));
        if (plan is null) return NotFound();
        return View(new PlanChangeImpactViewModel(plan.Code, plan.PublicName,
            string.Equals(access?.EffectivePlanCode, plan.Code, StringComparison.OrdinalIgnoreCase),
            ["Seus dados existentes serão preservados.", "Os limites passam a seguir o plano escolhido após a confirmação do pagamento."],
            "Revise os detalhes antes de continuar."));
    }

    [HttpPost("account/plan/change/{planCode}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmChange(string planCode, CancellationToken ct)
    {
        if (this.CurrentClientId() == Guid.Empty || this.CurrentUserId() == Guid.Empty) return Forbid();
        var available = (await catalog.GetPublicCatalogAsync(ct)).Any(x => x.Code.Equals(planCode, StringComparison.OrdinalIgnoreCase));
        if (!available) return NotFound();
        return planCode.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(Usage))
            : RedirectToAction("Index", "Plans", new { selected = planCode });
    }
}
