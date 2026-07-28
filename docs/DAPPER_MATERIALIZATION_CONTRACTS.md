# Contratos de materialização Dapper

`ClientPlanAccessRow` é o contrato explícito entre Npgsql e domínio: construtor padrão, propriedades públicas, aliases SQL exatos, `DateOnly?` para `grace_period_until` e mapping explícito. O domínio não é materializado diretamente. Códigos conhecidos são `free`, `ritmo` e `evolucao`; desconhecidos mantêm o contratado para diagnóstico, geram log/contador e reduzem o efetivo para `free`.

Testes reais devem definir `HABITFLOW_TEST_CONNECTION_STRING` apontando obrigatoriamente para banco terminado em `_tests`. A preparação usa apenas o runner canônico; não combine `migrate.sql` e `script_completo.sql`. Casos mínimos: `date` nulo, hoje, futuro, passado e ano bissexto, além dos três planos, código desconhecido e cliente ausente.
