# Dados de Teste em PRD

Produção não deve conter dados falsos permanentes. Testes usam uma conta smoke identificada, nunca usuários reais.

## Regras
- Todo dado deve ter prefixo `[SMOKE]`.
- Todo documento deve conter `isSmokeTest`, `createdBySmokeTest`, `smokeRunId` e `environment: production`.
- Scripts exigem `--confirm-prd`, `ALLOW_PRD_SMOKE_DATA=true` e `PRD_SMOKE_TEST_UID`.

## Criar e limpar
```bash
ALLOW_PRD_SMOKE_DATA=true PRD_SMOKE_TEST_UID=<uid> node scripts/seed/create-prd-smoke-data.js --confirm-prd
ALLOW_PRD_SMOKE_DATA=true PRD_SMOKE_TEST_UID=<uid> SMOKE_RUN_ID=<id> node scripts/seed/cleanup-prd-smoke-data.js --confirm-prd
```
