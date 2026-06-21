# CHATBOT.md — Assistente HabitFlow v1.9

O Assistente HabitFlow usa arquitetura híbrida:

- **rules**: base local de intents e conhecimento.
- **backend_ai**: preparado para IA futura via Firebase Functions.
- **hybrid**: modo padrão; tenta backend autenticado e mantém fallback por regras.

## Function principal
`askHabitFlowAssistant` recebe mensagem e contexto, exige autenticação, sanitiza entrada, limita 1000 caracteres, bloqueia conteúdo sensível, salva histórico controlado em `users/{uid}/supportConversations` e retorna resposta segura.

## Intents
Inclui: greeting, product_overview, create_habit_help, edit_habit_help, archive_habit_help, restore_habit_help, complete_habit_help, streak_help, progress_help, plan_free_help, premium_help, payment_help, login_help, privacy_help, lgpd_help, support_help, whatsapp_help, email_help, mnsoft_info, bug_report, technical_problem, pwa_help, sensitive_request, security_attack_request e unknown.

## Base de conhecimento
Cobre produto, hábitos, progresso, planos, conta, suporte MNSOFT, segurança/LGPD e problemas comuns.
