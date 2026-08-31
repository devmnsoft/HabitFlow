-- HabitFlow v6.18.8: pontos verificáveis e ranking explicitamente opt-in.
set search_path to habitflow, public;
create table if not exists gamification_points_ledger(
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 source_type varchar(30) not null check(source_type in('completion','routine','consistency','reversal')),
 source_id uuid not null, points integer not null check(points between -100 and 100), local_date date not null,
 occurred_at timestamptz not null, idempotency_key varchar(160) not null,
 unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_gamification_points_owner_period on gamification_points_ledger(client_id,user_id,local_date desc);
create table if not exists gamification_leaderboard_preferences(
 client_id uuid not null references clients(id), user_id uuid not null references users(id), is_opted_in boolean not null default false,
 scope varchar(20) not null default 'Private' check(scope in('Private','Team','General')), public_name varchar(40) not null,
 team_id uuid null references teams(id), updated_at timestamptz not null default now(), primary key(client_id,user_id),
 check(is_opted_in or scope='Private')
);
create index if not exists ix_gamification_leaderboard_visible on gamification_leaderboard_preferences(client_id,scope,team_id) where is_opted_in;
insert into achievement_definitions(code,name,description,icon,criterion,category,rarity) values
 ('first_habit','Primeiro passo','Você criou seu primeiro hábito ativo.','sparkles','first_active_habit','começo','comum'),
 ('consistency_30','Presença de 30 dias','Trinta dias de presença, respeitando pausas.','calendar','streak_30','consistência','especial'),
 ('routine_completed','Rotina completa','Você concluiu uma rotina real.','check-circle','rotina','especial'),
 ('consistent_week','Semana consistente','Uma semana saudável e consistente.','sun','consistência','especial'),
 ('return_after_pause','Bom retorno','Você voltou depois de uma pausa, sem punição.','heart','return_after_pause','bem-estar','comum'),
 ('template_used','Começo guiado','Você iniciou um hábito usando um template.','layout','começo','comum')
on conflict(code) do update set name=excluded.name,description=excluded.description,criterion=excluded.criterion,is_active=true;
