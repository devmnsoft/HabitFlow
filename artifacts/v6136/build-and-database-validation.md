# Build e banco — v6.13.6

> Data: 2026-08-17 UTC
> SHA inicial: `ee66750e8bac3a3df199e33df49af6ae5a3f958c`

## Resultado
- `dotnet --info`, clean, restore e build: **não executados**, pois `dotnet` não existe no PATH (`command not found`).
- Publish: **não executado** pela mesma limitação.
- PostgreSQL real e migrations: **não executados**, pois `pwsh` e `psql` não existem no PATH e não há connection string fornecida.

## Correção aplicada
O validador PostgreSQL foi atualizado para v6.13.6 e agora exige template ativo/publicado, escopo completo de favoritos e as tabelas de favoritos, onboarding, relatórios e revisão semanal, além das verificações anteriores. Ele continua cobrindo banco novo, existente e rerun.

## Decisão
Validação pendente; nenhum resultado foi inferido ou declarado como aprovado sem execução.
