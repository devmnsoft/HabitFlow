using System.Net;
using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Options;
using WebPush;

namespace HabitFlow.Infrastructure;

public sealed class PushNotificationSender(IOptions<PushNotificationOptions> options) : IPushNotificationSender
{
    public async Task<PushSendResult> SendAsync(PushSubscriptionRecord subscription, PushPayload payload, CancellationToken ct = default)
    {
        var config = options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
            return new(false, false, "push_not_configured");
        try
        {
            using var client = new WebPushClient();
            await client.SendNotificationAsync(new(subscription.Endpoint, subscription.P256Dh, subscription.Auth),
                JsonSerializer.Serialize(new { payload.Title, payload.Body, payload.Url }),
                new VapidDetails(config.Subject, config.PublicKey, config.PrivateKey), ct);
            return new(true, false);
        }
        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        { return new(false, true, "subscription_expired"); }
        catch (WebPushException) { return new(false, false, "provider_rejected"); }
    }
}
