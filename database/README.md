# Banco de dados HabitFlow

## Scripts principais

- `database/migrate.sql`: aplica migrations incrementais durante desenvolvimento controlado.
- `database/script_completo.sql`: cria todo o schema `habitflow` em um banco PostgreSQL limpo, sem usuários fake e sem dependência de `\i`.
- `database/script_completo_dev.sql`: complemento exclusivo de desenvolvimento, cria `admin@habitflow.local` e `user@habitflow.local` com senha `Admin@123`.

## Validação

```powershell
scripts/database/validate-script-completo.ps1
```

```bat
scripts\database\validate-script-completo.bat
```

Nunca execute `script_completo_dev.sql` em produção.
# Banco HabitFlow
Execute `psql -U postgres -d habitflow -f database/migrate.sql`. Use `database/seeds/seed-dev.sql` somente em desenvolvimento; produção deve usar `seed-prd.sql` e secrets locais.

## Atualização v4.1 - qualidade ASP.NET e SQL completo

- Use `database/script_completo.sql` para criar um banco PostgreSQL limpo de forma completa e sem seeds de usuários de desenvolvimento.
- Use `database/script_completo_dev.sql` somente em desenvolvimento, após o script completo, para criar `admin@habitflow.local` e `user@habitflow.local` com senha documentada `Admin@123`.
- Em Docker, execute `docker compose up -d --build`; a aplicação fica em `http://localhost:5097` e o PostgreSQL em `localhost:5432`.
- Em Windows/IIS, publique com `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows`, configure `appsettings.Production.json` fora do Git e use o `web.config` publicado.
- Nunca use seed dev, `.env` real ou `appsettings.Production.json` real em produção.
- Valide o banco com `scripts/database/validate-script-completo.ps1` ou `scripts/database/validate-script-completo.bat` quando `psql` estiver disponível.
