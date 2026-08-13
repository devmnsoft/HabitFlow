using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class NotificationActionUrlValidator
{
    private static readonly string[] Allowed = ["/dashboard", "/habits", "/goals", "/reminders", "/progress", "/notifications", "/onboarding"];
    public bool IsSafe(string? url) => string.IsNullOrEmpty(url) ||
        (Uri.TryCreate(url, UriKind.Relative, out _) && url[0] == '/' && !url.StartsWith("//") && Allowed.Any(prefix => url.Equals(prefix, StringComparison.OrdinalIgnoreCase) || url.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) || url.StartsWith(prefix + "?", StringComparison.OrdinalIgnoreCase)));
}

public sealed class NotificationCenterService(INotificationRepository repository, NotificationActionUrlValidator urls, TimeProvider clock)
{
    public Task<NotificationPage> SearchAsync(NotificationQuery query, CancellationToken ct = default) => repository.SearchAsync(query with { Page=Math.Max(1,query.Page), PageSize=Math.Clamp(query.PageSize,1,50) },ct);
    public Task<int> UnreadCountAsync(Guid userId,CancellationToken ct=default)=>repository.CountUnreadAsync(userId,ct);
    public Task<bool> SetReadAsync(Guid clientId,Guid userId,Guid id,bool read,CancellationToken ct=default)=>repository.SetReadAsync(clientId,userId,id,read,clock.GetUtcNow().UtcDateTime,ct);
    public Task<bool> SetArchivedAsync(Guid clientId,Guid userId,Guid id,bool archived,CancellationToken ct=default)=>repository.SetArchivedAsync(clientId,userId,id,archived,clock.GetUtcNow().UtcDateTime,ct);
    public Task<int> MarkAllAsReadAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>repository.MarkAllAsReadAsync(clientId,userId,clock.GetUtcNow().UtcDateTime,ct);
    public Task<int> ArchiveReadAsync(Guid clientId,Guid userId,CancellationToken ct=default)=>repository.ArchiveReadAsync(clientId,userId,clock.GetUtcNow().UtcDateTime,ct);
    public string? SafeAction(string? actionUrl)=>urls.IsSafe(actionUrl)?actionUrl:null;
}
