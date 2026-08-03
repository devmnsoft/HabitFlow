\if :{?environment}
\else
  \error 'environment é obrigatória'
\endif
\if :{?superadmin_email}
\else
  \error 'superadmin_email é obrigatória'
\endif
\if :{?superadmin_name}
\else
  \error 'superadmin_name é obrigatória'
\endif
\if :{?superadmin_password_hash}
\else
  \error 'superadmin_password_hash é obrigatória'
\endif
\if :{?actor}
\else
  \error 'actor é obrigatório'
\endif
\if :{?correlation_id}
\else
  \error 'correlation_id é obrigatório'
\endif
SELECT :'environment' = 'Development' AS is_development \gset
\if :is_development
\else
  \error 'Script recusado fora de Development'
\endif
SELECT length(:'superadmin_password_hash') > 0 AS has_hash \gset
\if :has_hash
\else
  \error 'superadmin_password_hash não pode ser vazio'
\endif
BEGIN;
INSERT INTO habitflow.users(id,name,email,password_hash,role,account_status,risk_status,plan,plan_status,onboarding_completed,client_id,session_version,must_change_password,created_at,updated_at)
VALUES(gen_random_uuid(),:'superadmin_name',lower(:'superadmin_email'),:'superadmin_password_hash','SuperAdmin','Active','Normal','Free','Active',true,null,1,true,now(),now())
ON CONFLICT(email) DO UPDATE SET name=excluded.name,password_hash=excluded.password_hash,role='SuperAdmin',account_status='Active',client_id=null,session_version=habitflow.users.session_version+1,must_change_password=true,updated_at=now();
WITH target AS (SELECT id FROM habitflow.users WHERE email=lower(:'superadmin_email'))
INSERT INTO habitflow.user_role_assignments(id,user_id,role_id,client_id,created_at)
SELECT gen_random_uuid(),target.id,r.id,null,now() FROM target CROSS JOIN habitflow.roles r WHERE r.code='super_admin'
ON CONFLICT DO NOTHING;
INSERT INTO habitflow.role_permissions(role_id,permission_code) SELECT id,'Platform.FullAccess' FROM habitflow.roles WHERE code='super_admin' ON CONFLICT DO NOTHING;
UPDATE habitflow.password_reset_tokens SET revoked_at=now() WHERE user_id=(SELECT id FROM habitflow.users WHERE email=lower(:'superadmin_email')) AND used_at IS NULL AND revoked_at IS NULL;
INSERT INTO habitflow.system_audit_logs(id,user_id,user_email,severity,source,action,message,metadata,created_at)
SELECT gen_random_uuid(),id,'***','Info','DevelopmentSql','superadmin.dev_provisioned','Credencial de desenvolvimento provisionada',jsonb_build_object('actor',:'actor','correlationId',:'correlation_id'),now()
FROM habitflow.users WHERE email=lower(:'superadmin_email');
COMMIT;
