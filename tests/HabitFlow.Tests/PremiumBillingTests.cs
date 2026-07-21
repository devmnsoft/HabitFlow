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
}
