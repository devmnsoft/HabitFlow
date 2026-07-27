set search_path to habitflow, public;

create table if not exists habitflow.plan_prices(
 id uuid primary key, plan_id uuid not null references habitflow.plans(id), billing_cycle varchar(40) not null check(billing_cycle in ('Monthly','Yearly')),
 amount numeric(12,2) not null check(amount>=0), currency varchar(10) not null default 'BRL', is_active boolean not null default true,
 valid_from timestamp not null default now(), valid_until timestamp null, created_at timestamp not null default now(), unique(plan_id,billing_cycle,valid_from));
create table if not exists habitflow.feature_catalog(
 code varchar(120) primary key, name varchar(160) not null, description text null, value_type varchar(30) not null check(value_type in ('Boolean','Integer','String')),
 category varchar(80) not null, is_active boolean not null default true, created_at timestamp not null default now());
create table if not exists habitflow.plan_features(
 plan_id uuid not null references habitflow.plans(id), feature_code varchar(120) not null references habitflow.feature_catalog(code), bool_value boolean null,
 int_value integer null, string_value text null, created_at timestamp not null default now(), updated_at timestamp not null default now(), primary key(plan_id,feature_code));

insert into habitflow.plan_prices(id,plan_id,billing_cycle,amount,currency,valid_from)
select v.id,p.id,v.cycle,v.amount,'BRL',timestamp '2026-01-01' from (values
 ('20000000-0000-0000-0000-000000000001'::uuid,'free','Monthly',0.00),('20000000-0000-0000-0000-000000000002'::uuid,'free','Yearly',0.00),
 ('20000000-0000-0000-0000-000000000003'::uuid,'ritmo','Monthly',19.90),('20000000-0000-0000-0000-000000000004'::uuid,'ritmo','Yearly',199.00),
 ('20000000-0000-0000-0000-000000000005'::uuid,'evolucao','Monthly',49.90),('20000000-0000-0000-0000-000000000006'::uuid,'evolucao','Yearly',499.00)) v(id,code,cycle,amount)
join habitflow.plans p on p.code=v.code on conflict do nothing;

insert into habitflow.feature_catalog(code,name,value_type,category) values
 ('active_habits_limit','Hábitos ativos','Integer','Limites'),('users_limit','Pessoas da conta','Integer','Limites'),('full_habit_library','Biblioteca completa','Boolean','Hábitos'),
 ('reminders_per_habit','Lembretes por hábito','Integer','Hábitos'),('active_goals_limit','Objetivos ativos','Integer','Objetivos'),('custom_categories','Categorias personalizadas','Boolean','Hábitos'),
 ('basic_reports','Resumo semanal','Boolean','Relatórios'),('advanced_reports','Relatórios avançados','Boolean','Relatórios'),('report_export_csv','Exportação CSV','Boolean','Relatórios'),
 ('report_print','Impressão de relatórios','Boolean','Relatórios'),('full_history','Histórico completo','Boolean','Histórico'),('shared_routines','Rotinas compartilhadas','Boolean','Compartilhamento'),
 ('shared_goals','Objetivos compartilhados','Boolean','Compartilhamento'),('client_admin_dashboard','Painel da conta','Boolean','Conta'),('consolidated_reports','Relatórios consolidados','Boolean','Relatórios'),
 ('user_invitations','Convites de pessoas','Boolean','Conta'),('priority_support','Suporte prioritário','Boolean','Suporte'),('internal_communications','Comunicações internas','Boolean','Conta')
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category;

insert into habitflow.plan_features(plan_id,feature_code,bool_value,int_value)
select p.id,f.code,case when f.value_type='Boolean' then (case when p.code='free' then f.code in ('basic_reports','internal_communications') when p.code='ritmo' then f.code not in ('shared_routines','shared_goals','client_admin_dashboard','consolidated_reports','user_invitations','priority_support') else true end) end,
case when f.value_type='Integer' then case f.code when 'users_limit' then case when p.code='evolucao' then 5 else 1 end when 'active_habits_limit' then case when p.code='free' then 5 else -1 end when 'reminders_per_habit' then case when p.code='free' then 1 else -1 end when 'active_goals_limit' then case when p.code='free' then 1 else -1 end end end
from habitflow.plans p cross join habitflow.feature_catalog f where p.code in ('free','ritmo','evolucao') on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,int_value=excluded.int_value,updated_at=now();

