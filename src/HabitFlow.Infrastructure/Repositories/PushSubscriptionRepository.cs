using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class PushSubscriptionRepository(SqlExecutor db) : IPushSubscriptionRepository
{
    public Task UpsertAsync(PushSubscriptionRecord s, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.push_subscriptions(id,client_id,user_id,endpoint,p256dh,auth,device_name,is_active,created_at,last_seen_at)
        values(@Id,@ClientId,@UserId,@Endpoint,@P256Dh,@Auth,@DeviceName,@IsActive,@CreatedAt,@LastSeenAt)
        on conflict(client_id,user_id,endpoint) do update set p256dh=excluded.p256dh,auth=excluded.auth,device_name=excluded.device_name,is_active=true,revoked_at=null,last_seen_at=excluded.last_seen_at
        """, s, ct);
    public async Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid clientId, Guid userId, bool activeOnly=false, CancellationToken ct=default) =>
        (await db.QueryAsync<PushSubscriptionRecord>("select id,client_id,user_id,endpoint,p256dh,auth,device_name,is_active,created_at,last_seen_at from habitflow.push_subscriptions where client_id=@clientId and user_id=@userId and (not @activeOnly or is_active) order by created_at desc",new{clientId,userId,activeOnly},ct)).ToList();
    public async Task<bool> RemoveAsync(Guid clientId,Guid userId,Guid subscriptionId,CancellationToken ct=default) => await db.ExecuteAsync("update habitflow.push_subscriptions set is_active=false,revoked_at=now() where id=@subscriptionId and client_id=@clientId and user_id=@userId and is_active",new{clientId,userId,subscriptionId},ct)==1;
    public Task DeactivateAsync(Guid clientId,Guid userId,Guid subscriptionId,CancellationToken ct=default) => db.ExecuteAsync("update habitflow.push_subscriptions set is_active=false,revoked_at=now() where id=@subscriptionId and client_id=@clientId and user_id=@userId",new{clientId,userId,subscriptionId},ct);
    public async Task<PushNotificationPreference> GetPreferenceAsync(Guid clientId,Guid userId,CancellationToken ct=default) => await db.QuerySingleOrDefaultAsync<PushNotificationPreference>("select client_id,user_id,push_enabled,internal_enabled,quiet_start,quiet_end,maximum_per_day,paused_until,habit_reminders,daily_summary,weekly_summary,timezone,language from habitflow.notification_preferences where client_id=@clientId and user_id=@userId",new{clientId,userId},ct) ?? new(clientId,userId,false,true,null,null,5,null);
    public Task SavePreferenceAsync(PushNotificationPreference p,CancellationToken ct=default) => db.ExecuteAsync("insert into habitflow.notification_preferences(client_id,user_id,push_enabled,internal_enabled,quiet_start,quiet_end,maximum_per_day,paused_until,habit_reminders,daily_summary,weekly_summary,timezone,language,updated_at) values(@ClientId,@UserId,@PushEnabled,@InternalEnabled,@QuietStart,@QuietEnd,@MaximumPerDay,@PausedUntil,@HabitReminders,@DailySummary,@WeeklySummary,@Timezone,@Language,now()) on conflict(client_id,user_id) do update set push_enabled=excluded.push_enabled,internal_enabled=excluded.internal_enabled,quiet_start=excluded.quiet_start,quiet_end=excluded.quiet_end,maximum_per_day=excluded.maximum_per_day,paused_until=excluded.paused_until,habit_reminders=excluded.habit_reminders,daily_summary=excluded.daily_summary,weekly_summary=excluded.weekly_summary,timezone=excluded.timezone,language=excluded.language,updated_at=now()",p,ct);
    public Task RecordAttemptAsync(PushDeliveryAttempt a,CancellationToken ct=default) => db.ExecuteAsync("insert into habitflow.push_delivery_attempts(id,client_id,user_id,subscription_id,status,error_code,attempted_at) values(@Id,@ClientId,@UserId,@SubscriptionId,@Status,@ErrorCode,@AttemptedAt)",a,ct);
}
