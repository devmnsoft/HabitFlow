-- HabitFlow v5.6.2 enum/check-constraint validation.
-- Safe script: reads constraint definitions and validates canonical C# enum text values without permanent writes.

select n.nspname as schema_name,
       c.relname as table_name,
       con.conname as constraint_name,
       pg_get_constraintdef(con.oid) as definition
from pg_constraint con
join pg_class c on c.oid = con.conrelid
join pg_namespace n on n.oid = c.relnamespace
where n.nspname = 'habitflow'
  and c.relname in ('users','system_audit_logs','clients','support_tickets','lgpd_requests')
  and con.contype = 'c'
order by c.relname, con.conname;

with expected(table_name, column_name, value) as (
    values
    ('users','role','User'), ('users','role','Admin'),
    ('users','account_status','Active'), ('users','account_status','Blocked'), ('users','account_status','Suspended'), ('users','account_status','DeletedPending'),
    ('users','risk_status','Normal'), ('users','risk_status','Suspicious'), ('users','risk_status','Watchlist'),
    ('users','plan','Free'), ('users','plan','Premium'),
    ('users','plan_status','Active'), ('users','plan_status','Trial'), ('users','plan_status','Canceled'), ('users','plan_status','Inactive'), ('users','plan_status','PastDue'),
    ('system_audit_logs','severity','Info'), ('system_audit_logs','severity','Warning'), ('system_audit_logs','severity','Error'), ('system_audit_logs','severity','Critical'),
    ('clients','status','Active'), ('clients','status','Inactive'), ('clients','status','Blocked'),
    ('clients','plan','Free'), ('clients','plan','Premium'), ('clients','plan','Enterprise'),
    ('support_tickets','status','Open'), ('support_tickets','status','InProgress'), ('support_tickets','status','Resolved'), ('support_tickets','status','Closed'),
    ('lgpd_requests','type','Access'), ('lgpd_requests','type','Correction'), ('lgpd_requests','type','Deletion'), ('lgpd_requests','type','Portability'),
    ('lgpd_requests','status','Open'), ('lgpd_requests','status','InProgress'), ('lgpd_requests','status','Completed'), ('lgpd_requests','status','Rejected')
)
select * from expected order by table_name, column_name, value;
