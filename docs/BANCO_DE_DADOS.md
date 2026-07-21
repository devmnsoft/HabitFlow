# BANCO_DE_DADOS

HabitFlow v4 usa ASP.NET Core 10, DDD, Clean Architecture, Dapper, PostgreSQL, Bootstrap 5 e JavaScript Vanilla.

- Código principal: src/.
- Banco: database/migrations e database/seeds.
- Docker: porta 5097 para a aplicação e PostgreSQL 16.
- IIS: publicar em publish/windows com web.config e App Pool No Managed Code.
- Segurança: sem secrets no Git, sem stack trace em produção, cookies seguros, BCrypt e SQL parametrizado.
- LGPD: exportação e exclusão são registradas em habitflow.lgpd_requests.
- Legado Firebase: preservado como referência em legacy-firebase/ quando aplicável e não usado como backend principal.

## Atualização v4.1 - qualidade ASP.NET e SQL completo

- Use `database/script_completo.sql` para criar um banco PostgreSQL limpo de forma completa e sem seeds de usuários de desenvolvimento.
- Use `database/script_completo_dev.sql` somente em desenvolvimento, após o script completo, para criar `admin@habitflow.local` e `user@habitflow.local` com senha documentada `Admin@123`.
- Em Docker, execute `docker compose up -d --build`; a aplicação fica em `http://localhost:5097` e o PostgreSQL em `localhost:5432`.
- Em Windows/IIS, publique com `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows`, configure `appsettings.Production.json` fora do Git e use o `web.config` publicado.
- Nunca use seed dev, `.env` real ou `appsettings.Production.json` real em produção.
- Valide o banco com `scripts/database/validate-script-completo.ps1` ou `scripts/database/validate-script-completo.bat` quando `psql` estiver disponível.

## v4.2

Inclui a migration `012_habit_recurrence_reports_notifications.sql` com novos campos em `habitflow.habits` e tabelas `habit_week_days`, `notifications` e `user_reports`. Os scripts completos foram atualizados para PostgreSQL limpo.

## v4.3 Admin Operacional

A migration `013_admin_operacional.sql` adiciona campos operacionais em `habitflow.users`, notas administrativas, histórico de exportações e snapshots de dashboard. O `script_completo.sql` inclui a migration para instalação limpa sem depender de includes externos.
