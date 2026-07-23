-- Development-oriented backfill. Review manually before production use.
set search_path to habitflow;

insert into habitflow.clients(id, name, legal_name, document, plan, status, is_active, created_at, updated_at)
select gen_random_uuid(), 'Cliente Demonstração HabitFlow', 'Cliente Demonstração HabitFlow', '00000000000000', 'Free', 'Active', true, now(), now()
where not exists (select 1 from habitflow.clients where document = '00000000000000');

with demo as (select id from habitflow.clients where document = '00000000000000' limit 1)
update habitflow.users u set client_id = demo.id, updated_at = now()
from demo
where u.client_id is null and u.role <> 'SuperAdmin';

update habitflow.habits h set client_id = u.client_id, updated_at = now()
from habitflow.users u where h.user_id = u.id and h.client_id is null and u.client_id is not null;
update habitflow.habit_completions c set client_id = u.client_id
from habitflow.users u where c.user_id = u.id and c.client_id is null and u.client_id is not null;
update habitflow.notifications n set client_id = u.client_id
from habitflow.users u where n.user_id = u.id and n.client_id is null and u.client_id is not null;
update habitflow.user_reports r set client_id = u.client_id
from habitflow.users u where r.user_id = u.id and r.client_id is null and u.client_id is not null;
update habitflow.support_tickets t set client_id = u.client_id
from habitflow.users u where t.user_id = u.id and t.client_id is null and u.client_id is not null;
