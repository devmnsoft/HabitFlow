# Convenções de banco do HabitFlow

- O schema oficial do sistema é `habitflow`.
- Nenhuma tabela do sistema deve ser criada em `public`.
- Toda query Dapper deve usar nome qualificado, por exemplo `habitflow.users`.
- O `Search Path=habitflow` pode existir na connection string, mas nunca é a única proteção.
- Migrations e scripts precisam usar `habitflow.nome_tabela` em `create table`, `insert`, `update`, `delete`, `from`, `join`, foreign keys, índices e constraints.
- `database/script_completo.sql` é autônomo, de produção e não contém seed de usuários de teste.
- `database/script_completo_dev.sql` é apenas desenvolvimento e deve rodar depois do script completo.
- Índices e constraints novos usam prefixo claro, como `ix_habitflow_users_email` e `ck_habitflow_users_role`.
- Dados de teste precisam ser explicitamente marcados como desenvolvimento e nunca misturados em produção.

## Verificação de tabelas indevidas em public

```sql
select table_schema, table_name
from information_schema.tables
where table_schema = 'public'
  and table_name in ('users','habits','habit_completions','support_tickets','support_messages','system_audit_logs','admin_audit_logs','system_settings','lgpd_requests','billing_events','notifications','user_reports');
```

Também é possível executar `scripts/database/check-sql-schema-prefix.ps1` e `database/validate_schema_habitflow.sql`.
