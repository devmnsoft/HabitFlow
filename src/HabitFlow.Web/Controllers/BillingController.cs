using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class BillingController(SubscriptionService subscriptions, PaymentCheckoutService checkout, IPaymentTransactionRepository transactions, ILogger<BillingController> logger) : Controller
{
    [HttpGet("billing")]
    [HttpGet("account/billing")]
    public async Task<IActionResult> Index(CancellationToken ct)
    { var userId = this.CurrentUserId(); ViewBag.Subscription = await subscriptions.GetUserSubscriptionAsync(userId, ct); return View(await transactions.ListByUserAsync(userId, ct)); }

    [HttpPost("billing/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string planCode, string billingCycle, CancellationToken ct)
    {
        logger.LogInformation("plans.cta.clicked Cta=checkout Plan={PlanCode} Cycle={BillingCycle}", planCode, billingCycle);
        if (!Enum.TryParse<BillingCycle>(billingCycle, true, out var cycle)) { TempData["Error"] = "Ciclo inválido."; return RedirectToAction("Index", "Plans"); }
        var result = await checkout.StartCheckoutAsync(this.CurrentUserId(), User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty, User.Identity?.Name ?? "Usuário", planCode, cycle, ct);
        if (result.IsFailure) { logger.LogWarning("plans.checkout.unavailable Plan={PlanCode} Cycle={BillingCycle} Code={Code}", planCode, billingCycle, result.Error.Code); TempData["Error"] = result.Error.Code == "payment.not_configured" ? "Pagamento ainda não configurado neste ambiente. Fale com o suporte para registrar seu interesse." : result.Error.Message; return RedirectToAction("Index", "Plans"); }
        logger.LogInformation("plans.checkout.started Plan={PlanCode} Cycle={BillingCycle}", planCode, billingCycle);
        return Redirect(result.Value!.CheckoutUrl);
    }
    [HttpGet("billing/return/success")] public IActionResult Success() => View("Return", "Recebemos seu retorno. Seu plano será atualizado assim que o pagamento for confirmado com segurança.");
    [HttpGet("billing/return/pending")] public IActionResult Pending() => View("Return", "Identificamos um pagamento pendente. Seus dados continuam aqui e avisaremos quando ele for confirmado.");
    [HttpGet("billing/return/failure")] public IActionResult Failure() => View("Return", "Pagamento não aprovado. Seu plano atual não foi alterado.");

    [HttpPost("account/billing/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string reason, CancellationToken ct)
    {
        var subscription = await subscriptions.GetUserSubscriptionAsync(this.CurrentUserId(), ct);
        if (subscription is null) return NotFound();
        var result = await subscriptions.CancelSubscriptionAsync(subscription.Id, reason, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Cancelamento programado. O Premium permanece ativo até o fim do período já pago."
            : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
