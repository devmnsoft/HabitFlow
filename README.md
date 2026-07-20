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
