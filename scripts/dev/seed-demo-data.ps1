[CmdletBinding()] param([string]$Email='demo@habitflow.local',[string]$ConnectionString=$env:ConnectionStrings__DefaultConnection,[switch]$ConfirmDevelopment)
$ErrorActionPreference='Stop'
if($env:ASPNETCORE_ENVIRONMENT -ne 'Development' -and -not $ConfirmDevelopment){throw 'Development was not detected. Review the target and rerun with -ConfirmDevelopment.'}
if([string]::IsNullOrWhiteSpace($ConnectionString)){throw 'ConnectionStrings__DefaultConnection is required.'}
$parts=@{}; foreach($item in $ConnectionString-split';'){if($item-match'^\s*([^=]+)=(.*)$'){$parts[$matches[1].Trim().ToLowerInvariant()]=$matches[2].Trim()}}
$env:PGHOST=$parts['host'];$env:PGPORT=$(if($parts['port']){$parts['port']}else{'5432'});$env:PGUSER=$(if($parts['username']){$parts['username']}else{$parts['user id']});$env:PGPASSWORD=$parts['password'];$env:PGDATABASE=$parts['database']
$sql=@'
do $seed$
declare u uuid; c uuid; h uuid; i int;
begin
 select id,client_id into u,c from habitflow.users where lower(email)=lower(:'email');
 if u is null or c is null then raise exception 'Tenant-bound development user % was not found', :'email'; end if;
 for i in 1..5 loop
   h := md5(u::text||':demo-habit:'||i)::uuid;
   insert into habitflow.habits(id,user_id,name,color,category,is_archived,created_at,updated_at,client_id,start_date,frequency_type,sort_order)
   values(h,u,'Hábito demo '||i,'#198754','Bem-estar',false,now(),now(),c,current_date,'Daily',i) on conflict(id) do nothing;
 end loop;
 for i in 1..2 loop
   insert into habitflow.user_goals(id,client_id,user_id,title,description,target_type,target_value,start_date,end_date,status,color,icon)
   values(md5(u::text||':demo-goal:'||i)::uuid,c,u,'Objetivo demo '||i,'Dados locais descartáveis','HabitCompletions',10,current_date,current_date+30,'Active','#198754','target') on conflict(id) do nothing;
   h := md5(u::text||':demo-habit:'||i)::uuid;
   insert into habitflow.habit_reminders(id,client_id,user_id,habit_id,reminder_time,days_of_week)
   values(md5(u::text||':demo-reminder:'||i)::uuid,c,u,h,('08:0'||i)::time,array[1,2,3,4,5]) on conflict(id) do nothing;
 end loop;
 insert into habitflow.habit_completions(id,habit_id,user_id,completed_date,created_at)
 select md5(u::text||':demo-completion:'||i)::uuid,md5(u::text||':demo-habit:'||i)::uuid,u,current_date-i,now() from generate_series(1,3)i on conflict do nothing;
 insert into habitflow.habit_template_favorites(client_id,user_id,template_id)
 select c,u,id from habitflow.habit_templates where status='Published' order by sort_order nulls last limit 2 on conflict do nothing;
 insert into habitflow.notifications(id,user_id,type,title,message,is_read,created_at,client_id,category,deduplication_key)
 select md5(u::text||':demo-notification:'||i)::uuid,u,'System','Notificação demo '||i,'Conteúdo local para validação visual.',false,now()-(i||' hours')::interval,c,'Product','v6131-demo-'||i from generate_series(1,3)i on conflict do nothing;
end $seed$;
'@
$sql|& psql -X -v ON_ERROR_STOP=1 -v email=$Email
if($LASTEXITCODE){throw 'Demo seed failed.'}; Write-Host 'Idempotent local demo data applied: habits, goals, reminders, notifications, progress and template favorites.'
