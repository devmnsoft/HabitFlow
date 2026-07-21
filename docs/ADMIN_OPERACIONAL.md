# Admin Operacional v4.3

A versão `v4.3-AdminOperacional-Metrics-LGPD-Support` consolida o painel administrativo para operação real do HabitFlow.

## Áreas
- Dashboard executivo em `/admin`.
- Gestão de usuários em `/admin/users` e `/admin/users/{id}`.
- Suporte em `/admin/support`.
- LGPD em `/admin/lgpd`.
- Logs em `/admin/logs/system` e `/admin/logs/admin`.
- Leads e financeiro em `/admin/leads` e `/admin/finance`.

## Segurança
Todas as rotas administrativas exigem role `Admin`; POSTs usam antiforgery token, motivo obrigatório em ações sensíveis, auditoria administrativa e mensagens amigáveis sem stack trace.
