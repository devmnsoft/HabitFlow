# Release candidate v6.14.3

- SHA inicial: `37af819f3b2722d15757d77061f1272f5c748e61`
- Ambiente solicitado: Windows / PowerShell 7, indisponível no container atual.
- Runner: implementado; execução bloqueada antes do início.
- Build/publish: não executados (`dotnet` ausente).
- Migrations: não executadas (`pwsh` e `psql` ausentes).
- Startup e smoke público: não executados.
- Provisionamento, seed e smoke autenticado: não executados.
- Jornada MVP e regras Free/Ritmo: não validadas.
- Mobile: não validado; nenhuma screenshot fabricada.
- Checks npm: security scan, testes e audit executados; consultar resultado da entrega.

## P0s pendentes

- Executar o runner em `C:\MNSOFT\HabitFlow` com PowerShell 7, .NET 10, PostgreSQL/psql e credencial local real.
- Concluir build, migrations, runtime, smokes, jornada, regras mutacionais e checklist mobile.

## Decisão

**Release não aprovada — P0 pendente**
