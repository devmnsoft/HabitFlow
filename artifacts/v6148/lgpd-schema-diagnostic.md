# Diagnóstico do schema LGPD

## Inspeção estática

A migration 063 declara `habitflow.user_privacy_consents`, `habitflow.privacy_request_events`, o índice e o trigger esperados. O erro `42P01` informado demonstra drift no banco afetado: o código e o stream canônico esperam uma relação que esse banco não possui.

## Banco local/runtime

**Não executado neste ambiente.** O contêiner não possui `psql`, credencial local ou acesso ao banco Windows indicado no chamado. Portanto, não se fabricou resultado para `schema_migrations`, `information_schema` ou colunas.

A migration 066 repara bancos nos quais a 063 foi registrada sem que suas relações tenham permanecido no schema.
