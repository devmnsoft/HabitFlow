# HabitFlow v3 Migration

Documento da migração HabitFlow v3 para ASP.NET Core + DDD + Dapper + PostgreSQL.

## Diretrizes
- Firebase deixa de ser backend principal.
- PostgreSQL usa schema `habitflow` e scripts SQL versionados.
- Secrets devem ser configurados por ambiente, nunca versionados.
- A porta de desenvolvimento da aplicação ASP.NET Core é 5097.
- Docker publica a aplicação em 5097:8080.
- Windows/IIS usa `src/HabitFlow.Web/web.config` e `dotnet publish -c Release -o publish/windows`.

## Legado
O legado Firebase permanece como referência para comparação funcional e futura migração de dados.
