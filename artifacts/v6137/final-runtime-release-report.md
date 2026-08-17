# Relatório final de runtime/release — v6.13.7

- SHA inicial: `2fc10090ee53ee6bce9275f6a35f5c7f345367a6`.
- SHA final: o commit que contém este relatório (consultar `git rev-parse HEAD` após o commit).
- Ambiente: Ubuntu 24.04.4, Node v24.15.0/npm 11.4.2; .NET, pwsh, psql, PostgreSQL e navegador autenticado indisponíveis.
- Build/publish: bloqueados; não aprovados.
- Migrations: bloqueadas; não aprovadas.
- Startup/rotas públicas: não executados.
- Smoke autenticado/jornada: não executados.
- Regras de plano: não executadas em runtime; preços não alterados.
- Mobile: não executado; screenshots não fabricadas.
- Bugs corrigidos: destinos v6.13.7 dos validadores e cobertura operacional do smoke autenticado.
- Verificações disponíveis: security scan, testes existentes, audit e nove `node --check` aprovados.
- Segredos/binários/publish: nenhum adicionado.
- Testes novos: nenhum criado.

## Decisão de release

**NÃO APROVADO.** Os P0 ambientais e de execução real estão listados em `final-backlog.md`. O próximo módulo recomendado continua sendo fechamento P0 em host Windows/PostgreSQL com navegador, antes de criar novos testes ou funcionalidades.
