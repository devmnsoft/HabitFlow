# v6.12.4 — relatório final de validação em runtime

Data: 2026-08-12 (UTC)

## Status honesto da release

Os critérios de aceite de runtime **não foram satisfeitos neste ambiente**. Este relatório não usa “aprovado” para qualquer etapa que dependa de .NET, PostgreSQL, browser ou autenticação real.

## O que o PR #125 corrigiu de fato (inspeção do commit `bcad297`)

- fornecimento de `start_date` no INSERT de hábitos;
- projeção canônica `HabitSql.Columns`/`AliasedColumns` em vez de `select h.*` para materialização de `Habit`;
- migration `065_v6123_crud_contract_backfill.sql`;
- normalização e transação nos serviços de criação/edição;
- testes de contrato e specs Playwright de CRUD/no-500.

## Ambiente

| Ferramenta | Resultado |
|---|---|
| SO | Ubuntu 24.04.4 LTS, x86_64 |
| .NET | não instalado (`dotnet: command not found`) |
| PostgreSQL/psql | não instalado (`psql: command not found`) |
| Node | `v24.15.0` |
| npm | `11.4.2` |
| Playwright package | `1.54.1` (dependência local da suíte) |
| Browser Playwright | não instalado; todos os mirrors responderam HTTP 403 |

A tentativa de instalar os pré-requisitos com `apt-get` falhou por HTTP 403 do proxy obrigatório. A tentativa de `npx playwright install` também falhou por HTTP 403 nos mirrors oficiais.

## Build, testes e publish

- `dotnet clean`, `restore`, `build`, `test` e `publish`: não executados porque o executável `dotnet` não existe.
- `npm install`: passou usando o lock/cache disponível.
- `npm run security:scan`: passou.
- `npm test`: passou.
- `npm audit --omit=dev`: passou com 0 vulnerabilidades.
- `node --check` nos cinco arquivos solicitados: passou.

## Migrations

- Banco novo: não executado; PostgreSQL/psql indisponível.
- Banco existente: não executado; PostgreSQL/psql indisponível.
- Rerun: não executado; PostgreSQL/psql indisponível.
- Migration 065: inspecionada, mas não executada.

Detalhes: `artifacts/v6124/migrations-runtime-report.md`.

## CRUD e fluxos funcionais

| Área | Resultado |
|---|---|
| Hábitos | não executado em browser/banco |
| Objetivos | não executado em browser/banco |
| Lembretes | não executado em browser/banco |
| Notificações | não executado em browser/banco |
| Privacidade | não executado em browser/banco |
| Planos | não executado em browser |
| Busca | não executado em browser |

O servidor ASP.NET não pôde ser iniciado sem .NET. Não foi criado storage state e nenhuma credencial/token foi incluída no repositório.

## Playwright

`npx playwright test` foi realmente iniciado (não apenas listado): a suíte descobriu 848 testes. Os testes que exigiam autenticação foram ignorados sem storage state e os testes iniciados falharam imediatamente porque o Chromium não estava instalado. Isso é resultado **falho**, não aprovação. O HTML report/test-results gerado localmente permanece ignorado pelo Git por conter artifacts de execução.

## Auditoria Dapper e segurança

A varredura estática confirmou projeção canônica nas materializações de `Habit`, escopo crítico nos repositórios dos módulos e antiforgery nos POSTs de navegador solicitados. Detalhes e exceções administrativas documentadas: `artifacts/v6124/dapper-final-audit.md`.

## Bugs encontrados e corrigidos

- Nenhum bug de aplicação foi corrigido sem reprodução real; alterar código com base apenas em hipótese contrariaria a missão.
- Foram identificadas limitações de infraestrutura (ausência de SDK, banco, psql e browsers; downloads bloqueados por 403). Elas não são bugs do HabitFlow.

## Melhorias funcionais pequenas

Não implementadas: a missão determina que ocorram somente após o CRUD real passar, condição que não foi atingida.

## Pendências reais

1. Executar toda a matriz em um runner com .NET 10 SDK, PostgreSQL/psql e acesso aos browsers Playwright.
2. Criar banco novo, migrar snapshot existente e executar rerun.
3. Iniciar o servidor em `http://localhost:5097` com conexão real.
4. Gerar storage state efêmero fora do Git e executar os cenários autenticados.
5. Executar CRUD no navegador e validar registros no banco.
6. Somente depois, implementar e capturar screenshots das melhorias UX propostas.
