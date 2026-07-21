## v5.1 Premium Visual QA

Esta versão melhora Home, Dashboard, Central de Ajuda, tour guiado, checklist de primeiros passos, scripts de validação visual e fallback seguro para a marca MNSOFT. A execução sem Docker permanece preservada na porta 5097.


## Banco PostgreSQL v4.5

Todas as tabelas do HabitFlow ficam no schema `habitflow` e as queries usam nomes qualificados como `habitflow.users`. Para validar:

```powershell
scripts/database/check-sql-schema-prefix.ps1
scripts/database/validate-schema-habitflow.ps1 -Database habitflow
```

O endpoint `/health/db` retorna status detalhado do banco sem expor secrets. O Admin Operacional possui a página `/admin/database`.


## v4.4-WindowsIIS-Production-NoDocker
- Operação sem Docker formalizada.
- Scripts Windows para validação de ambiente, PostgreSQL, backup/restore, publicação IIS, rollback e smoke tests.
- Health checks /health, /health/db e /health/version.
- Diagnóstico Admin em Sistema > Ambiente.
- Migration 014 com habitflow.deployment_events.
- Documentação Windows/IIS sem Docker ampliada.

# HabitFlow v4
Reescrita limpa do HabitFlow em ASP.NET Core 10, DDD, Clean Architecture, Dapper, PostgreSQL, Bootstrap 5 e JavaScript Vanilla.

## Stack
ASP.NET Core MVC/Razor, Dapper/Npgsql, PostgreSQL 16, Docker, Cookie Authentication, BCrypt, auditoria, LGPD, Telegram backend e WhatsApp configurável.

## Rodar com Docker
`docker compose up -d --build` e acesse `http://localhost:5097`.

## Rodar sem Docker
Instale .NET 10 SDK e PostgreSQL, execute migrations em `database/migrate.sql`, depois `dotnet run --project src/HabitFlow.Web`.

## IIS / Windows
Publique com `scripts/publisher/publish-aspnet-windows.ps1`; configure Hosting Bundle, App Pool sem código gerenciado e `appsettings.Production.json` local não versionado.

## Usuários dev
`admin@habitflow.local` e `user@habitflow.local`. Senhas documentadas para dev são `Admin@123` e `User@123`; seeds armazenam BCrypt.

## Segurança e LGPD
Sem EF, sem Firebase como backend principal, SQL parametrizado, cookies HttpOnly/SameSite, secrets fora do Git, logs sanitizados e solicitações LGPD persistidas.

## Legado Firebase
O legado está documentado em `docs/LEGACY_FIREBASE.md` e preservado em `legacy-firebase/` quando aplicável.

## Atualização v4.1 - qualidade ASP.NET e SQL completo

- Use `database/script_completo.sql` para criar um banco PostgreSQL limpo de forma completa e sem seeds de usuários de desenvolvimento.
- Use `database/script_completo_dev.sql` somente em desenvolvimento, após o script completo, para criar `admin@habitflow.local` e `user@habitflow.local` com senha documentada `Admin@123`.
- Em Docker, execute `docker compose up -d --build`; a aplicação fica em `http://localhost:5097` e o PostgreSQL em `localhost:5432`.
- Em Windows/IIS, publique com `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows`, configure `appsettings.Production.json` fora do Git e use o `web.config` publicado.
- Nunca use seed dev, `.env` real ou `appsettings.Production.json` real em produção.
- Valide o banco com `scripts/database/validate-script-completo.ps1` ou `scripts/database/validate-script-completo.bat` quando `psql` estiver disponível.

### v4.2 — UserExperience Habit Recurrence Reports

A versão v4.2 adiciona recorrência de hábitos (diária, dias úteis, finais de semana e dias personalizados), meta semanal, lembrete opcional, observações, notificações internas, relatórios pessoais e exportação CSV protegida contra CSV injection.

## v4.3 Admin Operacional

O HabitFlow inclui painel administrativo operacional em `/admin`, gestão de usuários, suporte, LGPD, logs, métricas, leads Premium, financeiro inicial, exportações CSV seguras e migration `013_admin_operacional.sql`. A aplicação continua usando ASP.NET Core, Dapper e PostgreSQL na porta 5097.

## v4.6 Premium Payments Billing Automation

O HabitFlow agora possui planos reais Free/Premium, tela `/plans`, área `/billing`, checkout Mercado Pago configurável, webhook `/webhooks/mercadopago`, histórico financeiro e Admin Financeiro. Sem token Mercado Pago configurado, o checkout falha de forma segura e não ativa pagamento real.

## HabitFlow em uma frase

HabitFlow é um sistema simples para criar, acompanhar e manter hábitos no dia a dia. Você cadastra seus hábitos, marca o que concluiu e acompanha sua evolução com sequências, metas e relatórios fáceis de entender.

### Como começar

1. Acesse a Home e clique em **Começar grátis**.
2. Escolha um objetivo no onboarding: saúde, estudos, produtividade, bem-estar ou organização.
3. Adicione um hábito pronto e acompanhe o próximo passo no Dashboard.

### Rodar sem Docker

Use `dotnet restore`, `dotnet build` e `dotnet run --project src/HabitFlow.Web`. A porta padrão permanece 5097 e Docker continua opcional.

### Publicar no IIS

Publique com `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows` e configure o site no IIS apontando para a pasta publicada, sem commitar `publish/`.

### Validar banco

Execute `database/script_completo.sql` em banco limpo e depois `database/validate_schema_habitflow.sql`. Todas as tabelas do produto devem ficar no schema `habitflow`.

## v5.0 Acessibilidade e MNSOFT

- Preferências de visualização em `/profile/accessibility`.
- Modo alto contraste, fonte maior e redução de animações.
- Logo oficial MNSOFT esperada em `src/HabitFlow.Web/wwwroot/brand/mnsoft/logo-mnsoft-oficial.png`; binários devem ser adicionados manualmente.
- Execução sem Docker e porta 5097 preservadas.
