using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Domain.Billing;

namespace HabitFlow.Tests;

public sealed class BillingV6179Tests
{
    private static readonly string Root = RepositoryRootLocator.Find();

    [Fact]
    public void Default_configuration_is_manual_and_checkout_is_disabled()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var payments = json.RootElement.GetProperty("Payments");
        Assert.False(payments.GetProperty("Enabled").GetBoolean());
        Assert.Equal("Manual", payments.GetProperty("Provider").GetString());
        Assert.Equal("comercial@mnsoft.com.br", payments.GetProperty("CommercialEmail").GetString());
    }

    [Fact]
    public void Commercial_subscription_states_cover_safe_lifecycle()
    {
        var states = Enum.GetNames<SubscriptionStatus>();
        Assert.Contains("Trialing", states);
        Assert.Contains("PaymentPending", states);
        Assert.Contains("ManualReview", states);
        Assert.Contains("Active", states);
        Assert.Contains("PastDue", states);
        Assert.Contains("Canceled", states);
        Assert.Contains("Expired", states);
    }

    [Fact]
    public void Sanitizer_redacts_payment_secrets_recursively()
    {
        var safe = new PaymentMetadataSanitizer().Sanitize("""{"token":"secret","nested":{"card_number":"4111","status":"approved"}}""");
        Assert.DoesNotContain("secret", safe);
        Assert.DoesNotContain("4111", safe);
        Assert.Contains("approved", safe);
        Assert.Contains("[REDACTED]", safe);
    }

    [Fact]
    public void Sanitizer_does_not_echo_or_throw_for_malformed_provider_payload()
    {
        var safe = new PaymentMetadataSanitizer().Sanitize("{\"token\":\"secret\"");
        Assert.Equal("{\"invalidPayload\":true}", safe);
        Assert.DoesNotContain("secret", safe);
    }

    [Fact]
    public void Mercado_pago_signature_rejects_malformed_payload_and_ambiguous_headers()
    {
        Assert.False(MercadoPagoService.ValidateSignature("not-json", "ts=1,v1=abc", "request", "secret"));
        Assert.False(MercadoPagoService.ValidateSignature("{\"data\":{\"id\":\"1\"}}", "ts=1,ts=2,v1=abc", "request", "secret"));
    }

    [Fact]
    public void Payment_lifecycle_covers_pending_and_chargeback_without_paid_access()
    {
        Assert.Contains(nameof(PaymentStatus.Pending), Enum.GetNames<PaymentStatus>());
        Assert.Contains(nameof(PaymentStatus.ChargedBack), Enum.GetNames<PaymentStatus>());
        Assert.Contains(nameof(SubscriptionStatus.PaymentPending), Enum.GetNames<SubscriptionStatus>());
        Assert.Contains(nameof(SubscriptionStatus.ManualReview), Enum.GetNames<SubscriptionStatus>());
    }

    [Fact]
    public void Migration_is_additive_tenant_aware_and_idempotent()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database/migrations/078_v6179_real_billing_commercial.sql"));
        Assert.Contains("create table if not exists habitflow.billing_manual_adjustments", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists habitflow.billing_entitlement_usage", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("client_id uuid not null", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("correlation_id uuid not null", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("card_number", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plans_offer_a_real_contact_action_when_checkout_is_unavailable()
    {
        var view = File.ReadAllText(Path.Combine(Root, "src/HabitFlow.Web/Views/Plans/Partials/_CommercialPlanCard.cshtml"));
        Assert.Contains("checkoutEnabled", view);
        Assert.Contains("mailto:comercial@mnsoft.com.br", view);
        Assert.Contains("Falar com a MNSOFT", view);
    }
}
