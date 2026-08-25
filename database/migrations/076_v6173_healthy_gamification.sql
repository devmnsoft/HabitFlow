-- HabitFlow v6.17.3: healthy, personal gamification (tenant-safe and additive).
begin;
set local search_path to habitflow, public;

create table if not exists weekly_goals (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 name varchar(120) not null, week_start date not null, week_end date not null,
 target_completions integer not null check(target_completions between 1 and 100), current_completions integer not null default 0,
 status varchar(20) not null default 'Active' check(status in ('Active','Paused','Completed','Closed')),
 created_at timestamptz not null default now(), completed_at timestamptz,
 check(week_end=week_start+6), unique(client_id,user_id,week_start,name)
);
create table if not exists weekly_goal_habits (
 client_id uuid not null, user_id uuid not null, weekly_goal_id uuid not null references weekly_goals(id) on delete cascade,
 habit_id uuid not null references habits(id), created_at timestamptz not null default now(),
 primary key(client_id,user_id,weekly_goal_id,habit_id)
);
create table if not exists achievement_definitions (
 code varchar(80) primary key, name varchar(120) not null, description varchar(260) not null, icon varchar(40) not null,
 criterion varchar(120) not null, category varchar(30) not null check(category in ('começo','consistência','retorno','desafio','foco','premium')),
 rarity varchar(20) not null check(rarity in ('comum','especial','rara')), is_active boolean not null default true
);
create table if not exists user_achievements (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 achievement_code varchar(80) not null references achievement_definitions(code), status varchar(20) not null default 'Unlocked',
 unlocked_at timestamptz not null default now(), unique(client_id,user_id,achievement_code)
);
create table if not exists user_missions (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 code varchar(80) not null, title varchar(140) not null, description varchar(260) not null, target integer not null check(target>0),
 progress integer not null default 0 check(progress>=0), status varchar(20) not null default 'Active' check(status in ('Active','Completed','Dismissed')),
 local_date date not null, completed_at timestamptz, created_at timestamptz not null default now(), unique(client_id,user_id,code,local_date)
);
create table if not exists streak_freezes (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id), habit_id uuid not null references habits(id),
 frozen_date date not null, reason varchar(160), created_at timestamptz not null default now(), unique(client_id,user_id,habit_id,frozen_date)
);
create table if not exists gamification_events (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 event_type varchar(80) not null, entity_type varchar(40), entity_id uuid, idempotency_key varchar(160) not null,
 occurred_at timestamptz not null default now(), metadata jsonb not null default '{}'::jsonb, unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_weekly_goals_owner_week on weekly_goals(client_id,user_id,week_start desc);
create index if not exists ix_user_achievements_owner on user_achievements(client_id,user_id,unlocked_at desc);
create index if not exists ix_user_missions_owner_day on user_missions(client_id,user_id,local_date desc);

insert into achievement_definitions(code,name,description,icon,criterion,category,rarity) values
 ('first_habit','Primeiro passo','Você criou seu primeiro hábito.','sparkles','habits >= 1','começo','comum'),
 ('first_completion','Hoje conta','Você concluiu seu primeiro dia.','check-circle','completions >= 1','começo','comum'),
 ('consistency_3','Ritmo de 3 dias','Boa sequência esta semana.','flame','streak >= 3','consistência','comum'),
 ('consistency_7','Uma semana presente','Sete dias de passos consistentes.','calendar-check','streak >= 7','consistência','especial'),
 ('total_30','30 passos','Trinta conclusões construíram seu caminho.','footprints','completions >= 30','foco','especial'),
 ('challenge_started','Desafio aceito','Você iniciou seu primeiro desafio.','flag','challenges_started >= 1','desafio','comum'),
 ('challenge_completed','Desafio concluído','Você chegou ao fim do seu primeiro desafio.','trophy','challenges_completed >= 1','desafio','especial'),
 ('weekly_goal_completed','Semana no ritmo','Você alcançou sua primeira meta semanal.','target','weekly_goals_completed >= 1','foco','especial'),
 ('back_on_track','Ritmo retomado','Você retomou o ritmo após uma pausa.','refresh-cw','returned_after_pause','retorno','especial'),
 ('habit_30_days','Cuidado contínuo','Um hábito acompanhou você por 30 dias.','award','habit_age_days >= 30','consistência','rara')
on conflict(code) do update set name=excluded.name,description=excluded.description,icon=excluded.icon,criterion=excluded.criterion,category=excluded.category,rarity=excluded.rarity,is_active=true;

insert into feature_catalog(code,name,value_type,category,implementation_status,is_marketable) values
 ('weekly_goals','Metas semanais','Boolean','Progresso','Implemented',true),
 ('achievements','Conquistas','Boolean','Progresso','Implemented',true),
 ('advanced_achievements','Conquistas avançadas','Boolean','Progresso','Implemented',true),
 ('streak_freeze','Proteção de sequência','Boolean','Progresso','Implemented',true),
 ('missions','Missões pessoais','Boolean','Progresso','Implemented',true),
 ('progress_dashboard','Painel de progresso','Boolean','Progresso','Implemented',true)
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category,implementation_status='Implemented',is_marketable=true,is_active=true;
insert into plan_features(plan_id,feature_code,bool_value)
select p.id,f.code,case when f.code in ('streak_freeze','advanced_achievements') then p.code<>'free' else true end
from plans p cross join feature_catalog f where f.code in ('weekly_goals','achievements','advanced_achievements','streak_freeze','missions','progress_dashboard')
on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,updated_at=now();
commit;
