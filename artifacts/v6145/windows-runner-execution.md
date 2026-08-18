# Execução do runner Windows — v6.14.5

- Início/fim: 2026-08-18 (UTC)
- Ambiente disponível: contêiner Linux em `/workspace/HabitFlow`, não Windows
- Comando tentado (senha mascarada): `pwsh ./scripts/validation/run-release-candidate-local-windows.ps1 -BaseUrl 'http://localhost:5097' -ConnectionString 'Host=localhost;Port=5432;Database=habitflow_local;Username=postgres;Password=********' -DevEmail 'release-gate@habitflow.local'`
- Resultado: **não executado; P0 de ambiente**
- Erro completo: `/bin/bash: line 1: pwsh: command not found` (exit 127)

Não havia senha local disponível e nenhum segredo real foi usado ou persistido. O runner foi atualizado para escrever exclusivamente em `artifacts/v6145`, usar o banco temporário `habitflow_v6145_fresh` e rejeitar também `PostgresException` nos logs.
