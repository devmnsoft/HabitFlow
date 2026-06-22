# Auditoria Administrativa

## Coleção

`adminAuditLogs/{logId}`

Campos principais:

- `action`
- `adminUid`
- `adminEmail`
- `targetUserId`
- `targetUserEmail`
- `reason`
- `createdAt`
- `metadata`
- `environment`
- `appVersion`

## Ações auditadas

- Visualização e busca de usuários.
- Alteração manual de plano.
- Bloqueio, desbloqueio, suspensão e alteração de risco.
- Inclusão de notas administrativas.
- Exportações CSV.
- Visualização de métricas globais.

## Segurança e retenção

A coleção é bloqueada no Firestore Rules para clients. Consulta apenas por Functions administrativas. Retenção recomendada: 180 dias ou política comercial/LGPD definida pela operação.
