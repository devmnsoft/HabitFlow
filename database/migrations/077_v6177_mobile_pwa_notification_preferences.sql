-- HabitFlow v6.17.7: preferências completas e trilha de entrega multicanal.
begin;
alter table habitflow.notification_preferences add column if not exists habit_reminders boolean not null default true;
alter table habitflow.notification_preferences add column if not exists daily_summary boolean not null default false;
alter table habitflow.notification_preferences add column if not exists weekly_summary boolean not null default true;
alter table habitflow.notification_preferences add column if not exists timezone varchar(80) not null default 'America/Sao_Paulo';
alter table habitflow.notification_preferences add column if not exists language varchar(10) not null default 'pt-BR';
alter table habitflow.notification_preferences drop constraint if exists ck_notification_preferences_language;
alter table habitflow.notification_preferences add constraint ck_notification_preferences_language check (language in ('pt-BR','en-US'));
alter table habitflow.notification_preferences drop constraint if exists ck_notification_preferences_quiet_period;
alter table habitflow.notification_preferences add constraint ck_notification_preferences_quiet_period check ((quiet_start is null and quiet_end is null) or (quiet_start is not null and quiet_end is not null and quiet_start <> quiet_end));
-- Revogação preserva a auditoria e os endpoints continuam isolados pelo par client/user.
alter table habitflow.push_subscriptions add column if not exists revoked_at timestamptz;
create index if not exists ix_push_subscriptions_tenant_user_active on habitflow.push_subscriptions(client_id,user_id) where is_active and revoked_at is null;
-- A entrega existente representa BrowserPush. A chave opcional permite idempotência por ocorrência.
alter table habitflow.push_delivery_attempts add column if not exists channel varchar(20) not null default 'BrowserPush';
alter table habitflow.push_delivery_attempts add column if not exists scheduled_for timestamptz;
alter table habitflow.push_delivery_attempts add column if not exists reminder_id uuid;
alter table habitflow.push_delivery_attempts drop constraint if exists ck_push_delivery_channel;
alter table habitflow.push_delivery_attempts add constraint ck_push_delivery_channel check (channel in ('InApp','BrowserPush'));
create unique index if not exists ux_push_attempt_delivery on habitflow.push_delivery_attempts(client_id,user_id,subscription_id,reminder_id,channel,scheduled_for) where reminder_id is not null and scheduled_for is not null;
commit;
