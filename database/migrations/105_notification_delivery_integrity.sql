-- v6.29.0: honest notification states, daily logical quota, endpoint-safe push attempts.
begin;

alter table habitflow.notification_preferences drop constraint if exists ck_notification_preferences_language;
alter table habitflow.notification_preferences
    add constraint ck_notification_preferences_language
    check (language in ('pt-BR','en-US','es-ES','fr-FR'));

alter table habitflow.push_delivery_attempts drop constraint if exists push_delivery_attempts_status_check;
alter table habitflow.push_delivery_attempts drop constraint if exists ck_push_delivery_status;

update habitflow.push_delivery_attempts
   set status='ProviderAccepted'
 where status='Delivered';

alter table habitflow.push_delivery_attempts
    add constraint ck_push_delivery_status
    check (status in ('Processing','ProviderAccepted','Failed','SubscriptionInvalid','Suppressed'));

create table if not exists habitflow.notification_daily_usage (
    client_id uuid not null references habitflow.clients(id),
    user_id uuid not null references habitflow.users(id),
    local_date date not null,
    channel varchar(20) not null,
    used_count integer not null default 0 check (used_count >= 0),
    updated_at timestamptz not null default now(),
    primary key (client_id,user_id,local_date,channel),
    constraint ck_notification_daily_usage_channel check (channel in ('InApp','BrowserPush'))
);

alter table habitflow.reminder_dispatches drop constraint if exists ck_reminder_dispatches_status;

update habitflow.reminder_dispatches
   set status='ProviderAccepted'
 where status='Delivered';

alter table habitflow.reminder_dispatches
    add constraint ck_reminder_dispatches_status
    check (status in ('Pending','Processing','ProviderAccepted','Retry','Failed','Canceled','Suppressed'));

commit;
