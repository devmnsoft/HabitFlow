# SUPORTE.md — HabitFlow v1.9

## Canais
- Empresa: MNSOFT
- Razão social: MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA
- CNPJ: 18.160.057/0001-13
- E-mail: comercial@mnsoft.com.br
- Telegram administrativo: @hablitflowmns_bot

## Tickets
Tickets são criados por Firebase Functions em `supportTickets/{ticketId}` com protocolo `HF-YYYYMMDD-XXXX`.

Tipos: `bug`, `support`, `commercial`, `premium`, `other`.
Status: `open`, `in_progress`, `resolved`, `closed`.
Prioridades: `low`, `medium`, `high`, `critical`.

Usuários listam apenas seus tickets via `getMySupportTickets`. Admin Geral lista e atualiza chamados via Functions administrativas.

## Fluxo de bug
O chatbot coleta o que aconteceu, tela e impacto. Mensagens sensíveis são substituídas por `[mensagem bloqueada por segurança]`.
