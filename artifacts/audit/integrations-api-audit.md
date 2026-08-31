# Auditoria técnica — integrações e API (v6.19.1)

## Escopo revisado

Foram revisados autenticação/autorização, pipeline HTTP, controllers, contexto de usuário/tenant, contratos de repositório, projeções Dapper, migrations, scripts agregadores e views. Antes desta versão não existiam API pública versionada, chaves, webhooks ou feeds de calendário no código real.

## Achados e correções

- **Tenant:** consultas públicas agora exigem `client_id` e `user_id`; a chave autenticada injeta ambos como claims e toda consulta de negócio aplica os dois filtros.
- **Segredos:** chaves e tokens de calendário usam 256 bits aleatórios e somente SHA-256 é persistido. Prefixos são apenas identificadores; o valor integral aparece uma vez.
- **Autorização:** escopos são allowlisted e avaliados por endpoint. Requisições sem autenticação continuam negadas pelo framework.
- **Abuso:** API v1 recebeu janela fixa de 60 requisições/minuto, sem fila.
- **Dapper:** aliases SQL coincidem com os construtores posicionais de `ApiKeyRecord`, `CalendarFeed` e `IntegrationWebhook`; arrays PostgreSQL são materializados como `string[]`.
- **Calendário:** feed público é protegido por token forte, responde `no-store`, escapa texto ICS e não inclui notas ou outros dados sensíveis.
- **Webhooks:** schema contempla segredo cifrado, tentativa idempotente, status e retry. A configuração/envio permanece indisponível na UI até o entitlement Team e o dispatcher real estarem configurados; nenhum provedor fictício foi criado.
- **Importação/exportação:** jobs e índices foram preparados, mas a ação permanece explicitamente desabilitada até existir processamento real com preview, limites e entitlement.

## Pendências explícitas

O dispatcher HTTP assinado, editor de webhooks, endpoints de escrita, importador/exportador e documentação OpenAPI devem ser habilitados somente junto de testes de integração com PostgreSQL e política comercial confirmada. A interface informa o modo seguro em vez de simular sucesso.

## Evidência do ambiente

Em 2026-08-31, `npm ci`, `npm test`, `npm run security:scan`, `npm audit --omit=dev` e `git diff --check` concluíram com sucesso. O SDK `dotnet` não está instalado neste container (`command not found`), portanto clean/restore/build/test/publish .NET ficaram bloqueados por limitação real do ambiente. O clone também não possui remote `origin`, impedindo fetch/pull e abertura remota de PR; essas limitações não foram mascaradas.
