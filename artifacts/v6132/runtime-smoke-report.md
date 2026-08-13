# Smoke de runtime v6.13.2

Status geral: **Bloqueado**.

O ambiente não possui `dotnet`, `pwsh`, `psql`, Docker/PostgreSQL nem credencial GitHub. A instalação do SDK também foi bloqueada por HTTP 403. Consequentemente, a aplicação não foi iniciada e nenhuma resposta HTTP foi fabricada.

| Rotas | Status HTTP | Resultado | Erro | Correção |
|---|---:|---|---|---|
| Públicas (`/`, `/plans`, `/login`, `/register`, `/service-worker.js`, `/favicon.ico`) | — | Bloqueado | Runtime indisponível | Executar `validate-local-windows.ps1` em host preparado |
| Autenticadas (16 rotas do helper) | — | Bloqueado | Runtime, banco e sessão indisponíveis | Provisionar usuário Development e executar `smoke-authenticated-routes.ps1` |
