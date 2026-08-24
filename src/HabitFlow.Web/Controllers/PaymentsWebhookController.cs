using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[AllowAnonymous]
public sealed class PaymentsWebhookController(PaymentWebhookService webhooks) : ControllerBase
{
    [HttpPost("webhooks/mercadopago")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MercadoPago(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body); var payload = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await webhooks.ReceiveAsync(PaymentProvider.MercadoPago, payload, headers, ct);
        return result.IsSuccess ? Ok(new { received = true }) : BadRequest(new { received = true, error = result.Error.Code });
    }

    [HttpPost("billing/webhooks/mercadopago")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> MercadoPagoBilling(CancellationToken ct) => MercadoPago(ct);
}
