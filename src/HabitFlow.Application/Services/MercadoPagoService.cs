using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class MercadoPagoService(HttpClient http, IConfiguration config, ILogger<MercadoPagoService> logger) : IPaymentProviderService
{
    public async Task<Result<CheckoutPreference>> CreateCheckoutPreferenceAsync(CheckoutRequest request, Subscription subscription, Plan plan, CancellationToken ct = default)
    {
        var token = config["MercadoPago:AccessToken"]; if (string.IsNullOrWhiteSpace(token)) return Result<CheckoutPreference>.Failure("payment.not_configured", "Pagamento não configurado.");
        var baseUrl = (config["Payment:PublicBaseUrl"] ?? "http://localhost:5097").TrimEnd('/'); var notification = config["MercadoPago:NotificationUrl"]; if (string.IsNullOrWhiteSpace(notification)) notification = baseUrl + "/webhooks/mercadopago";
        var price = request.BillingCycle == BillingCycle.Yearly ? plan.PriceYearly : plan.PriceMonthly; if (price is null or <= 0) return Result<CheckoutPreference>.Failure("payment.invalid_amount", "Valor do plano inválido.");
        using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences"); msg.Headers.Authorization = new("Bearer", token);
        msg.Content = JsonContent.Create(new { items = new[] { new { title = plan.Name, quantity = 1, currency_id = plan.Currency, unit_price = price.Value } }, external_reference = $"habitflow:{request.UserId}:{subscription.Id}", notification_url = notification, back_urls = new { success = baseUrl + (config["Payment:SuccessUrl"] ?? "/billing/return/success"), pending = baseUrl + (config["Payment:PendingUrl"] ?? "/billing/return/pending"), failure = baseUrl + (config["Payment:FailureUrl"] ?? "/billing/return/failure") }, auto_return = "approved", metadata = new { user_id = request.UserId, subscription_id = subscription.Id, plan_code = plan.Code } });
        var response = await http.SendAsync(msg, ct); var body = await response.Content.ReadAsStringAsync(ct); if (!response.IsSuccessStatusCode) { logger.LogWarning("Mercado Pago recusou preference: {Status}", response.StatusCode); return Result<CheckoutPreference>.Failure("payment.provider_error", "Provedor de pagamento indisponível."); }
        using var doc = JsonDocument.Parse(body); var url = doc.RootElement.TryGetProperty("init_point", out var init) ? init.GetString() : doc.RootElement.GetProperty("sandbox_init_point").GetString(); var pref = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null; return string.IsNullOrWhiteSpace(url) ? Result<CheckoutPreference>.Failure("payment.checkout_url_missing", "Checkout indisponível.") : Result<CheckoutPreference>.Success(new(url!, pref));
    }
    public async Task<Result<ProviderPayment>> GetPaymentAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var token = config["MercadoPago:AccessToken"]; if (string.IsNullOrWhiteSpace(token)) return Result<ProviderPayment>.Failure("payment.not_configured", "Pagamento não configurado.");
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{Uri.EscapeDataString(providerPaymentId)}"); msg.Headers.Authorization = new("Bearer", token); var response = await http.SendAsync(msg, ct); var body = await response.Content.ReadAsStringAsync(ct); if (!response.IsSuccessStatusCode) return Result<ProviderPayment>.Failure("payment.lookup_error", "Não foi possível consultar pagamento.");
        using var doc = JsonDocument.Parse(body); var r = doc.RootElement; var raw = Read(r, "status") ?? "unknown"; var status = raw switch { "approved" => PaymentStatus.Approved, "pending" or "in_process" => PaymentStatus.Pending, "rejected" => PaymentStatus.Rejected, "cancelled" => PaymentStatus.Canceled, "refunded" => PaymentStatus.Refunded, _ => PaymentStatus.Unknown }; decimal? amount = r.TryGetProperty("transaction_amount", out var a) && a.TryGetDecimal(out var d) ? d : null; return Result<ProviderPayment>.Success(new(providerPaymentId, Read(r, "external_reference"), raw, status, amount, Read(r, "currency_id") ?? "BRL", Read(r, "preference_id")));
    }
    public Task<Result> ValidateWebhookAsync(string payload, IReadOnlyDictionary<string,string> headers, CancellationToken ct = default)
    {
        var secret = config["MercadoPago:WebhookSecret"]; if (string.IsNullOrWhiteSpace(secret)) return Task.FromResult(Result.Success());
        if (!headers.TryGetValue("x-signature", out var signature)) return Task.FromResult(Result.Failure("webhook.invalid_signature", "Assinatura ausente."));
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return Task.FromResult(signature.Contains(expected, StringComparison.OrdinalIgnoreCase) ? Result.Success() : Result.Failure("webhook.invalid_signature", "Assinatura inválida."));
    }
    private static string? Read(JsonElement e, string name) => e.TryGetProperty(name, out var v) ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText().Trim('"') : null;
}
