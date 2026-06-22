# HabitFlow v2.3 — Admin Global

O **Admin Global** é uma área do Admin Geral para operação segura de usuários, planos, leads Premium, métricas, funil, financeiro inicial, exportações e auditoria.

## Acesso e permissões

- A aba só é exibida para e-mails listados em `ADMIN_EMAILS` no frontend.
- Toda consulta global passa por Firebase Functions callable.
- O backend valida autenticação, Admin Geral, rate limit, payload e auditoria.
- O client não lê `users` globais, `adminAuditLogs`, `systemAuditLogs`, `billingEvents` ou `adminUserNotes` diretamente.

## Módulos

- **Usuários**: listagem paginada/limitada, busca por nome, e-mail ou UID, badges de plano, status e risco.
- **Perfil do usuário**: perfil administrativo, assinatura, hábitos resumidos, atividade recente, tickets, LGPD e notas.
- **Planos**: alteração manual via `adminUpdateUserPlan`, com motivo obrigatório e auditoria.
- **Bloqueio e risco**: atualização de `accountStatus` e `riskStatus` via backend.
- **Leads Premium**: identifica interesse Premium, limite free e Premium manual/dev.
- **Métricas e conversão**: cards executivos e estimativas de funil.
- **Financeiro inicial**: receita real R$ 0,00 enquanto pagamento real não estiver ativo; potencial mensal/anual estimado.
- **Exportações**: CSV de usuários, leads, tickets, LGPD e segurança com sanitização contra CSV injection.
- **Auditoria**: últimas ações administrativas em `adminAuditLogs`.

## Status de conta

- `active`: uso normal.
- `blocked`: autentica, mas a interface mostra bloqueio e Functions críticas podem negar.
- `suspended`: acesso restrito e orientação ao suporte.
- `deleted_pending`: conta em processo de exclusão LGPD.

## Segurança operacional

Ações críticas exigem motivo, registram `adminAuditLogs`, geram `systemAuditLogs` e podem enviar Telegram administrativo sem dados sensíveis.
