# Inventário do HEAD — v6.13.1

- **SHA inicial:** `0b3d3e81b1f5011c6bf86633f862336ba59e7b7b`
- **Último PR detectado:** PR #133 (`0b3d3e8`, merge de `codex/preparar-release-candidate-v6.13.0`).
- **Branch de trabalho:** `feature/v6131-real-pipeline-windows-postgres-runtime-validation`
- **Áreas críticas:** `HabitFlow.sln`, `src/HabitFlow.Web`, `src/HabitFlow.Infrastructure`, `database/migrations/001–065`, `scripts/database/run-migrations.sh`, workflows de CI e assets JavaScript principais.

## Ambiente local observado

| Dependência | Status |
|---|---|
| .NET SDK/runtime | Bloqueado — executável ausente |
| PostgreSQL/psql | Bloqueado — executável ausente |
| Docker | Bloqueado — executável ausente |
| PowerShell | Bloqueado — executável ausente |
| Node | Aprovado — v24.15.0 |
| npm | Aprovado — 11.4.2 |

## Pendências herdadas do PR #133

Build, publish, migrations reais, startup ASP.NET, sessão autenticada, jornadas, banco comercial, navegador e screenshots não haviam sido executados. Esta entrega cria caminhos locais e de CI para executá-los; os itens dependentes das ferramentas ausentes permanecem explicitamente bloqueados até uma execução do workflow ou de uma máquina Windows preparada.
