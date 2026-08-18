# Relatório final do release candidate — v6.14.5

- SHA inicial: `e80eaac5853f61f4fd8247bf2d815bfdee978385`
- SHA final: commit que contém este relatório (ver histórico Git)
- Ambiente solicitado: Windows / PowerShell 7 / PostgreSQL
- Ambiente efetivo: contêiner Linux; `dotnet`, `pwsh` e `psql` ausentes

| Gate | Resultado |
|---|---|
| Runner Windows | P0 — PowerShell ausente e host não é Windows |
| Build / publish | P0 — .NET ausente |
| Migrations / EXPLAIN | P0 — PowerShell, psql e PostgreSQL indisponíveis |
| Startup / smoke público | P0 — aplicação não iniciada |
| Usuário / smoke autenticado | P0 — dependências anteriores indisponíveis |
| Biblioteca / jornada MVP | P0 — sem runtime e navegador autenticado |
| Free / Ritmo / Evolução | P0 — sem execução mutacional |
| Mobile | P0 — sem navegador; nenhum screenshot fabricado |
| `npm run security:scan` | aprovado |
| `npm test` | aprovado |
| `npm audit --omit=dev` | aprovado; 0 vulnerabilidades |
| nove `node --check` solicitados | aprovados |

## Alteração realizada

O runner agora gera artifacts v6.14.5, publica em diretório v6.14.5, usa banco temporário v6.14.5 e rejeita `PostgresException` no log, além das assinaturas já existentes.

## P0s pendentes

Executar o runner em `C:\MNSOFT\HabitFlow` com .NET 10, PowerShell 7, psql/PostgreSQL, senha local não commitada e navegador. Somente essa execução pode fechar build, migrations, EXPLAIN, runtime, jornada e mobile.

## Decisão

**Release não aprovada — P0 pendente**
