-- HabitFlow v6.19.5: estrutura idempotente; a senha é criada somente pelo hasher BCrypt da aplicação.
begin;
insert into habitflow.clients(id,name,legal_name,document,email,plan,status,is_active,created_at,updated_at,person_type,document_type,document_raw,document_normalized,trade_name)
values('61950000-0000-4000-8000-000000000001','MNSOFT','MNSOFT','18160057000113','comercial@mnsoft.com.br','Enterprise','Active',true,now(),now(),'LegalPerson','CNPJ','18.160.057/0001-13','18160057000113','MNSOFT')
on conflict(id) do update set name='MNSOFT',legal_name='MNSOFT',is_active=true,updated_at=now();

insert into habitflow.permissions(code,name,description,category) values
('Platform.Health.View','Saúde do sistema','Visualização da saúde global','Platform'),
('Platform.Tenants.Block','Bloqueio de clientes','Bloqueio e desbloqueio auditado','Platform')
on conflict(code) do nothing;
insert into habitflow.role_permissions(role_id,permission_code)
select r.id,p.code from habitflow.roles r cross join habitflow.permissions p
where r.code='super_admin' and p.code like 'Platform.%' on conflict do nothing;
insert into habitflow.schema_migrations(id,name,applied_at) values('086','v6195_superadmin_bootstrap',now()) on conflict(id) do nothing;
commit;
