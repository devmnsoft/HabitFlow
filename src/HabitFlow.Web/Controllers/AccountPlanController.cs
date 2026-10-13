using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class AccountPlanController(
    PlanUsageService usage,
    IPlanCatalogRepository catalog,
    IClientRepository clients,
    IUserRepository users,
    SubscriptionService subscriptions,
    PaymentCheckoutService checkout,
    AuditService audit,
    ILogger<AccountPlanController> logger) : Controller
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
            [
                "Seus dados existentes serão preservados em qualquer troca de plano.",
                planCode.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase)
                    ? "O downgrade para o Gratuito tem efeito imediato: os limites do plano gratuito passam a valer."
                    : "Os recursos do plano pago ficam disponíveis assim que o pagamento for confirmado; até lá, permanecem travados."
            ],
            "Revise os detalhes antes de continuar."));
    }

    [HttpPost("account/plan/change/{planCode}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmChange(string planCode, CancellationToken ct)
    {
        var clientId = this.CurrentClientId();
        var userId = this.CurrentUserId();
        if (clientId == Guid.Empty || userId == Guid.Empty) return Forbid();
        var code = planCode.Trim().ToLowerInvariant();
        var available = (await catalog.GetPublicCatalogAsync(ct)).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (!available) return NotFound();

        var access = await catalog.GetClientAccessAsync(clientId, ct);
        if (string.Equals(access?.EffectivePlanCode, code, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Info"] = "Você já está neste plano.";
            return RedirectToAction(nameof(Usage));
        }

        // Downgrade para o Gratuito: efeito imediato — os limites do plano free passam a valer.
        // Nenhum dado é removido; apenas os entitlements são travados no plano gratuito.
        if (code == PlanCodes.Free)
        {
            var changed = await clients.ChangePlanAsync(clientId, code, "Downgrade solicitado pelo próprio usuário", userId, ct);
            var current = await subscriptions.GetUserSubscriptionAsync(userId, ct);
            if (changed && current is not null && current.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
                await subscriptions.CancelSubscriptionAsync(current.Id, "Downgrade para o plano gratuito", ct);
            if (changed)
            {
                await audit.LogAsync("account.plan_downgraded", "Usuário fez downgrade para o plano gratuito", AuditSeverity.Info,
                    userId, User.Identity?.Name, new { clientId, planCode = code }, ct);
                TempData["Success"] = "Plano alterado para Gratuito. Seus dados continuam preservados; os limites do plano gratuito passam a valer imediatamente.";
            }
            else TempData["Error"] = "Não foi possível alterar o plano agora. Tente novamente ou fale com o suporte.";
            return RedirectToAction(nameof(Usage));
        }

        // Upgrade / troca para plano pago: cria assinatura pendente e inicia o checkout real.
        // Sem confirmação de pagamento (webhook) os recursos pagos permanecem travados — nunca simulamos pagamento.
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Forbid();
        var cycle = BillingCycle.Monthly;
        var checkoutResult = await checkout.StartCheckoutAsync(userId, user.Email, user.Name, code, cycle, ct);
        if (checkoutResult.IsSuccess && !string.IsNullOrWhiteSpace(checkoutResult.Value?.CheckoutUrl))
            return Redirect(checkoutResult.Value.CheckoutUrl);

        var errorCode = checkoutResult.IsFailure ? checkoutResult.Error.Code : "checkout.unavailable";
        logger.LogWarning("Checkout indisponível para usuário {UserId} trocando para plano {PlanCode}: {Code}", userId, code, errorCode);
        TempData["Error"] = errorCode == "payment.checkout_unavailable"
            ? "O pagamento online ainda não está disponível neste ambiente. Sua solicitação foi registrada; fale com o comercial (comercial@mnsoft.com.br) para ativar o plano. Os recursos pagos ficam travados até a confirmação do pagamento."
            : "Não foi possível iniciar a troca de plano agora. Nenhum valor foi cobrado — tente novamente ou fale com o suporte.";
        return RedirectToAction(nameof(Usage));
    }
}
