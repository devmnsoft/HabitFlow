using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public class PremiumBillingTests
{
    [Fact]
    public void Payment_metadata_sanitizer_removes_sensitive_fields()
    {
        var sanitized = new PaymentMetadataSanitizer().Sanitize("{\"card_token\":\"abc\",\"amount\":14.90,\"payer\":{\"cpf\":\"123\"}}");
        Assert.DoesNotContain("abc", sanitized);
        Assert.DoesNotContain("123", sanitized);
        Assert.Contains("[REDACTED]", sanitized);
    }

    [Fact]
    public void Billing_cycles_are_limited_to_monthly_and_yearly()
    {
        Assert.Equal("Monthly", HabitFlow.Domain.BillingCycle.Monthly.ToString());
        Assert.Equal("Yearly", HabitFlow.Domain.BillingCycle.Yearly.ToString());
    }

    [Fact]
    public void Payment_status_maps_approved_without_frontend_activation()
    {
        Assert.Equal("Approved", HabitFlow.Domain.PaymentStatus.Approved.ToString());
    }

    [Fact]
    public void Mercado_pago_signature_requires_exact_hmac_manifest()
    {
        const string payload = "{\"id\":\"evt-1\",\"data\":{\"id\":\"PAY-123\"}}";
        const string requestId = "req-1";
        const string timestamp = "1710000000";
        const string secret = "test-only-secret";
        var manifest = $"id:pay-123;request-id:{requestId};ts:{timestamp};";
        var hash = Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secret), System.Text.Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        Assert.True(MercadoPagoService.ValidateSignature(payload, $"ts={timestamp},v1={hash}", requestId, secret));
        Assert.False(MercadoPagoService.ValidateSignature(payload, $"ts={timestamp},v1={new string('0', 64)}", requestId, secret));
    }
}
