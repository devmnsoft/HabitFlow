# QA v6.6.2

## Cobertura adicionada

Testes unitários cobrem Daily, Weekdays, Weekends, CustomWeekly, limites de criação/arquivamento, meses de 28/29/30/31 dias, dias sem agenda, sequência e denominador real. O repositório aplica `client_id` e `user_id`, agrupa duplicidades e usa aliases explícitos.

## Limitações do ambiente

SDK .NET, PostgreSQL/psql e navegador Playwright não estão instalados. Build, testes, integração PostgreSQL, teste funcional, publish e captura visual não foram executados localmente; devem ser validados pela CI. Nenhum screenshot foi incluído no commit.
