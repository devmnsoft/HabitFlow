namespace HabitFlow.Domain.Billing;

/// <summary>Provider-neutral contract. Implementations must redirect to a hosted checkout.</summary>
public interface IBillingGateway
{
    Task<BillingGatewayResult<CheckoutSessionResult>> CreateCheckoutSessionAsync(CheckoutSessionRequest request, CancellationToken ct = default);
    Task<BillingGatewayResult<BillingWebhookEvent>> ParseWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
}

public record BillingGatewayResult(bool Succeeded, string? ErrorCode = null, string? ErrorMessage = null);
public sealed record BillingGatewayResult<T>(bool Succeeded, T? Value = default, string? ErrorCode = null, string? ErrorMessage = null)
    : BillingGatewayResult(Succeeded, ErrorCode, ErrorMessage);
public sealed record CheckoutSessionRequest(Guid ClientId, Guid UserId, string PlanCode, string BillingCycle, string SuccessUrl, string CancelUrl, string Currency);
public sealed record CheckoutSessionResult(string ProviderSessionId, Uri HostedCheckoutUrl, DateTimeOffset? ExpiresAt);
public sealed record BillingWebhookEvent(string ProviderEventId, string EventType, string ResourceId, string PayloadHash);
public sealed class BillingProviderOptions
{
    public const string SectionName = "Billing";
    public string Provider { get; set; } = "MercadoPago";
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string Currency { get; set; } = "BRL";
    public int GracePeriodDays { get; set; } = 3;
}

/// <summary>Safe commercial switch. Disabled/manual mode never creates a paid entitlement.</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";
    public string Provider { get; set; } = "Manual";
    public bool Enabled { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string CommercialEmail { get; set; } = "comercial@mnsoft.com.br";
    public string? WhatsApp { get; set; }
}
