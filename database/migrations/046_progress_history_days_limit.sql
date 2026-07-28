begin;
insert into habitflow.feature_catalog(code,name,value_type,category)
values ('history_days_limit','Limite do histórico em dias','Integer','Histórico')
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category;

insert into habitflow.plan_features(plan_id,feature_code,int_value)
select id,'history_days_limit',case when code='free' then 90 else -1 end
from habitflow.plans where code in ('free','ritmo','evolucao')
on conflict(plan_id,feature_code) do update set int_value=excluded.int_value,updated_at=now();
commit;
