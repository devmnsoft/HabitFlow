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

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.
