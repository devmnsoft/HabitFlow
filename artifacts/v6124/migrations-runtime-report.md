# v6.12.4 — relatório de migrations em runtime

Data: 2026-08-12 (UTC)

## Ambiente e comandos

- Branch: `feature/v6124-real-crud-validation-runtime-fixes`.
- HEAD de origem: `d6b05cb15fc13ef73c7f69282d625c3bad407d33` (merge do PR #125).
- `psql --version`: não executado; o cliente não existe na imagem.
- Tentativa de preparação: `apt-get update && apt-get install -y postgresql postgresql-client curl ca-certificates`.
- Resultado da preparação: os repositórios Ubuntu, inclusive via HTTPS, responderam HTTP 403 pelo proxy obrigatório do ambiente. Nenhum pacote foi instalado.
- Runner oficial identificado: `scripts/database/run-migrations.sh`.

## Banco novo

**Não executado.** PostgreSQL e `psql` não estão disponíveis e não puderam ser instalados porque o proxy recusou os repositórios de pacotes com HTTP 403. Portanto não há resultado de schema, registro real da migration 065 ou consulta real de `start_date` a declarar.

## Banco existente

**Não executado.** Pelo mesmo limite de ambiente, não foi possível restaurar um snapshot, aplicar migrations pendentes nem executar `select count(*) from habitflow.habits where start_date is null`.

## Rerun

**Não executado.** Sem uma primeira execução real do runner, não existe rerun real nem evidência de idempotência em PostgreSQL.

## Inspeção estática (não substitui runtime)

A migration `065_v6123_crud_contract_backfill.sql`:

1. preenche `start_date` nulo com `created_at::date`;
2. configura `DEFAULT current_date`;
3. configura `NOT NULL`;
4. cria `ix_goal_habits_tenant_goal` com `IF NOT EXISTS`.

A migration 043 já cria e preenche `goal_habits.client_id` antes de torná-lo `NOT NULL`. Esses achados são somente inspeção de código e **não** são apresentados como migrations aprovadas.

## Erros encontrados e correções aplicadas

- Erro de infraestrutura: ausência de PostgreSQL/psql.
- Erro de preparação: HTTP 403 do proxy para `archive.ubuntu.com`, `security.ubuntu.com`, `apt.llvm.org` e `mise.jdx.dev`.
- Correções de migration: nenhuma, pois não houve evidência de falha SQL real que justificasse alterar regra ou histórico.
