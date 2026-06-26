# HabitFlow

HabitFlow v3 inicia a migração do frontend Firebase para uma aplicação ASP.NET Core MVC com DDD/Clean Architecture, Dapper e PostgreSQL. O legado Firebase/HTML/JS foi preservado como referência e a nova aplicação fica em `src/`.

## Rodar em desenvolvimento

```bash
dotnet restore
dotnet build
dotnet run --project src/HabitFlow.Web/HabitFlow.Web.csproj --urls http://localhost:5097
```

## Docker

```bash
docker compose up -d --build
```

## Banco

Execute os scripts em `database/migrations` ou `database/migrate.sql` com `psql`.
