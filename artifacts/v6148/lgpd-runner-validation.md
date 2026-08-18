# Validação adicionada ao runner LGPD

O runner agora consulta `information_schema.tables` logo após migrations para:

- `habitflow.user_privacy_consents`;
- `habitflow.privacy_request_events`.

Cada ausência encerra a execução como P0, nomeando a relação e orientando a execução das migrations. A evidência de aprovação só é escrita depois que ambas retornam exatamente uma tabela.

Execução real do runner permanece pendente em Windows, deliberadamente, porque o script recusa ambientes que não sejam Windows real.
