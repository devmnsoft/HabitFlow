# Erro PostgreSQL 28P01 no HabitFlow

## Causa
O código `28P01` significa que o usuário ou a senha configurados na connection string não conferem com o PostgreSQL local.

## Opção A — alterar a connection string
Use `src/HabitFlow.Web/appsettings.Development.local.json` (não versionado):

```txt
Host=localhost;Port=5432;Database=habitflow;Username=postgres;Password=SUA_SENHA_REAL;Search Path=habitflow;Pooling=true;Maximum Pool Size=50;Timeout=30;Command Timeout=60;Application Name=HabitFlow
```

## Opção B — alterar a senha do postgres

```sql
ALTER USER postgres WITH PASSWORD 'postgres';
```

## Opção C — criar usuário próprio

```sql
CREATE USER habitflow_user WITH PASSWORD 'habitflow_dev_123';
GRANT ALL PRIVILEGES ON DATABASE habitflow TO habitflow_user;
```

Depois configure `Username=habitflow_user;Password=habitflow_dev_123`.

Nunca commite senha real. Prefira `appsettings.Development.local.json` ou user-secrets.
