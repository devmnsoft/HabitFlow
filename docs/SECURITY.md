# SECURITY

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

## v4.3 Segurança administrativa

A v4.3 mantém autenticação por cookie, autorização por role Admin, proteção CSRF em POSTs administrativos, Dapper parametrizado, limitação de paginação, sanitização de CSV e middleware de status de conta.

## v4.6 Billing security

- Checkout não recebe tokens no frontend.
- Premium não é ativado pelo retorno do navegador.
- Webhook Mercado Pago salva somente payload sanitizado.
- Secrets devem vir de configuração segura do ambiente/IIS.
