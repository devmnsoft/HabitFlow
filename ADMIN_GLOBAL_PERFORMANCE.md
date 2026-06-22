# Performance do Admin Global

## Limites iniciais

- Listagem de usuários: limite padrão 20, máximo 100 na resposta operacional.
- Busca administrativa: limite máximo 50 resultados.
- Detalhe de usuário: hábitos e eventos limitados para resposta compacta.
- Exportações CSV: máximo 500 linhas por chamada na versão inicial.

## Paginação e índices

A versão inicial usa `collectionGroup('profile')` com limite e filtros simples em memória após resposta limitada. Para escala maior, criar índices/agregações incrementais para:

- `users/*/profile/main.createdAt`
- `users/*/profile/main.lastLoginAt`
- `users/*/profile/main.plan`
- `users/*/profile/main.accountStatus`
- `users/*/profile/main.riskStatus`
- `supportTickets.createdAt/status/userId`
- `lgpdRequests.createdAt/status/userId`
- `adminAuditLogs.createdAt/action/adminEmail/targetUserId`

## Futuras agregações

Quando a base crescer, migrar métricas globais para contadores incrementais em `appMetrics` atualizados por Functions/Cloud Scheduler, evitando varreduras operacionais.
