# Auditoria de banco e template solicitada para PlantaoPro

> Observação: o repositório local é HabitFlow (`/workspace/HabitFlow`), não PlantaoPro. A correção foi aplicada no produto encontrado, preservando ASP.NET Core, Dapper e PostgreSQL.

## Connection string atual

- Development: `Host=localhost;Port=5432;Database=habitflow;Username=postgres;Password=postgres;Search Path=habitflow;Pooling=true;Maximum Pool Size=50;Timeout=30;Command Timeout=60;Application Name=HabitFlow`.
- Banco esperado: `habitflow`.
- Schema esperado: `habitflow`.

## Scripts existentes e adicionados

- `database/script_completo.sql`: schema completo idempotente no schema `habitflow`.
- `database/create_database.sql`: criação do banco, para executar conectado ao banco administrativo `postgres`.
- `database/seed_dev.sql`: seed local com `admin@habitflow.local` e senha `Admin@123`.
- `scripts/database/create-habitflow-db.ps1|.bat`.
- `scripts/database/apply-script-completo.ps1|.bat`.
- `scripts/database/seed-dev.ps1|.bat`.
- `scripts/database/validate-db.ps1|.bat`.

## Problemas encontrados

- Ambiente local não possui `dotnet`, impedindo restore/build/test nesta execução.
- O serviço de login retornava mensagem genérica quando a causa era banco PostgreSQL ausente.
- O fluxo de autenticação tentava acionar auditoria mesmo quando a conexão ao banco nem abria, criando risco de erro secundário.

## Correções aplicadas

- Criado `PostgresErrorHelper` para detectar `SqlState=3D000`, falhas de conexão e mensagens amigáveis.
- `AuthService` agora retorna erro amigável para banco ausente e evita auditoria quando a conexão falha.
- `AuditService` permanece resiliente e registra warning em falhas de conexão sem derrubar a requisição.
- `DatabaseDiagnosticsService` retorna `unhealthy` com mensagem amigável em vez de propagar detalhes técnicos.
- Adicionados testes mínimos de resiliência sem depender de instância real do PostgreSQL.
