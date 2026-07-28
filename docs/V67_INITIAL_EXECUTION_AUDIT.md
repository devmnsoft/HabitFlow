# Auditoria inicial de execução — v6.7

Data da coleta: 28/07/2026 (UTC). Commit de partida: `a2e16919a8c8694d085a6a90dea8eee12633f023` (`correçã`). Branch de trabalho: `feature/production-core-convergence-v67`.

## Pré-voo e ambiente

| Verificação | Resultado observado |
|---|---|
| Árvore Git inicial | Limpa; nenhum `bin`, `obj`, `publish` ou `TestResults` rastreado |
| Sincronização com `main` | Indisponível: o clone não possui remote `origin` nem referência local `main`; a branch foi criada a partir do HEAD fornecido |
| SDK .NET | Indisponível no container (`dotnet: command not found`) |
| Clean e restore | Não executados pela ausência do SDK |
| Builds Domain/Application/Infrastructure/Web/solution | Não executados pela ausência do SDK |
| Testes .NET e compilação Razor | Não executados pela ausência do SDK |
| Publish Release | Não executado pela ausência do SDK |
| PostgreSQL | Serviço/credencial de teste não fornecidos nesta execução |
| Playwright | Não iniciado, pois o gate de build não pôde ser comprovado |
| Formatação | Não executada pela ausência do SDK |

O primeiro erro real foi ambiental: `/bin/bash: dotnet: command not found`. Não houve coleta de erros de compilação em cascata, e este documento não declara sucesso para comandos que não rodaram.

## Inventário e governança de migrations

O inventário inicial continha 46 migrations, de `001` a `046`, sem duplicidades e sem gaps. A próxima versão foi calculada a partir do filesystem e reservada como `047`, sem reutilizar ou modificar migrations anteriores.

A migration `047_schema_migration_governance.sql` acrescenta os metadados `filename` e `app_version`. O runner canônico `scripts/database/run-migrations.sh`:

- descobre os arquivos reais, ordenados pelo prefixo;
- rejeita versão duplicada e gap;
- calcula SHA-256;
- mantém `version`/`id`, nome, arquivo, checksum, data e versão da aplicação;
- usa `pg_advisory_xact_lock` e uma transação por migration;
- rejeita checksum divergente e exige forward fix;
- não reaplica uma versão registrada.

O CI foi ajustado para executar o runner duas vezes no PostgreSQL descartável, tornando o rerun uma evidência automática de idempotência quando o workflow for executado.

## Estado honesto dos gates

Somente a governança de migrations e seu contrato de CI foram alterados nesta etapa. As fases funcionais 1–5 não são declaradas concluídas. Build, testes, cenários PostgreSQL A/B/C, schema diff, concorrência, webhook, RBAC, Playwright e publicação IIS permanecem pendentes de execução em ambiente com .NET 10 e os serviços necessários.

