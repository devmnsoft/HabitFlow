# Provisionamento seguro do SuperAdmin

Os comandos `admin create-superadmin` e `admin promote-superadmin` usam apenas `--email`, `--name` e `--connection-string-name`. Argumentos de senha, hash, token ou secret são rejeitados. A configuração PostgreSQL vem do mecanismo padrão `ConnectionStrings:DefaultConnection`/`ConnectionStrings__DefaultConnection`.

A operação Dapper parametrizada é atômica e idempotente: mantém um usuário por e-mail, um assignment global ativo e a permissão `Platform.FullAccess`. Não altera plano financeiro. Auditorias guardam identificador, e-mail mascarado, ator local, motivo, correlation id e UTC, nunca credenciais.
