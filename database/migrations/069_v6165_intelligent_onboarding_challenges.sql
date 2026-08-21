-- habitflow:transaction=runner
-- HabitFlow v6.16.5: tenant-safe challenge foundation.
create table if not exists habitflow.user_challenges (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 habit_id uuid not null references habitflow.habits(id),
 name varchar(160) not null,
 description varchar(320) not null,
 duration_days integer not null constraint ck_user_challenges_duration check(duration_days in (7,30,90)),
 start_date date not null,
 end_date date not null,
 status varchar(20) not null default 'Active' constraint ck_user_challenges_status check(status in ('Active','Completed','Abandoned','Expired')),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(), completed_at timestamptz,
 constraint ck_user_challenges_dates check(end_date=start_date+(duration_days-1))
);
create index if not exists ix_user_challenges_owner on habitflow.user_challenges(client_id,user_id,status,created_at desc);
create unique index if not exists ux_user_challenges_active_habit on habitflow.user_challenges(client_id,user_id,habit_id) where status='Active';

-- The feature catalogue is honest: the 7-day flow is available to Free, while
-- longer challenges are implemented but enforced as Premium by the backend.
insert into habitflow.feature_catalog(code,name,value_type,category,implementation_status,is_marketable)
values
 ('challenge_7_days','Desafio de 7 dias','Boolean','Desafios','Implemented',true),
 ('challenge_30_days','Desafio de 30 dias','Boolean','Desafios','Implemented',true),
 ('challenge_90_days','Desafio de 90 dias','Boolean','Desafios','Implemented',true)
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category,implementation_status='Implemented',is_marketable=true;
insert into habitflow.plan_features(plan_id,feature_code,bool_value)
select p.id,f.code,(f.code='challenge_7_days' or p.code<>'free') from habitflow.plans p cross join habitflow.feature_catalog f
where f.code in ('challenge_7_days','challenge_30_days','challenge_90_days')
on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,updated_at=now();
