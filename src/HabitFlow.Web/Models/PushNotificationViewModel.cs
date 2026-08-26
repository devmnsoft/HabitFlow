using HabitFlow.Domain;
namespace HabitFlow.Web.Models;
public sealed record PushNotificationViewModel(bool PushConfigured, string PublicKey, PushNotificationPreference Preference, IReadOnlyList<PushSubscriptionRecord> Devices);
public sealed record PushSubscriptionRequest(string Endpoint, string P256Dh, string Auth, string? DeviceName);
