# Auditoria de Segurança — HabitFlow 2.0-Security

Data da auditoria: 2026-06-21.

## Pontos analisados
Frontend (`assets/js`), `firebase.js`, logger, error monitor, chatbot, admin, plans, Functions, `firestore.rules`, `firebase.json`, service worker, manifest, documentação, `.gitignore`, scripts e padrões de secrets.

## Riscos encontrados e correções
| Severidade | Risco | Correção aplicada |
|---|---|---|
| Alta | Hosting publicava a raiz e podia expor código fonte JS original. | `firebase.json` agora publica `dist/`. |
| Alta | Rules genéricas permitiam escrita ampla em `users/{uid}/...`. | Rules separadas por perfil, hábitos, usageEvents, billing e conversas. |
| Média | Ausência de build de produção sem source maps. | Vite configurado com minificação, `sourcemap:false` e scan de dist. |
| Média | App Check não estava preparado. | Criado `assets/js/app-check.js` com inicialização opcional. |
| Média | Service worker cacheava fontes JS originais. | Cache limitado a assets estáticos não sensíveis. |
| Média | Rate limit básico ausente em Functions sensíveis. | Adicionado rate limit por usuário em Functions principais. |
| Baixa | Ambiente definido dentro de plans. | Criado `assets/js/env.js` com versão e detecção dev/prod. |

## Pendências recomendadas
- Instalar dependências quando o registry permitir e executar build completo.
- Ativar App Check enforcement após teste.
- Migrar admin para custom claims.
- Validar CSP em navegador real.
- Implementar validação criptográfica real dos webhooks de pagamento.

## Auditoria v2.1-SecurityOps

| Item analisado | Risco encontrado | Severidade | Correção aplicada | Status | Pendência |
|---|---|---:|---|---|---|
| CI/CD | Ausência de gate automatizado de segurança | Alta | Criado `.github/workflows/security-ci.yml` com scans, build e validações | Concluído | Configurar branch protection no GitHub |
| Secrets | Risco de secrets versionados | Crítica | Reforçado `scripts/security-scan.js` | Concluído | Rotacionar qualquer secret que já tenha sido exposto antes |
| Dist | Risco de source maps e artefatos internos | Alta | Criado `scripts/security-dist-scan.js` | Concluído | Manter obfuscação/bundle em releases comerciais |
| Firestore Rules | Erro grosseiro pode expor dados | Crítica | Criado `scripts/validate-firestore-rules.js` e bloqueio client para coleções operacionais | Concluído | Testes em emulador na v2.2 |
| firebase.json | Risco de publicar raiz | Alta | Criado `scripts/validate-firebase-config.js` | Concluído | Revisar headers após mudanças de Auth |
| Functions | Abuso por chamadas repetidas | Alta | Rate limit em Functions críticas e eventos `rate_limit_exceeded` | Concluído | App Check enforcement em produção |
| Admin Geral | Falta de visão de risco | Média | Adicionado painel Segurança com eventos suspeitos | Concluído | Métricas persistentes de pipeline |
| LGPD | Falta de fluxo formal | Alta | Criadas solicitações de exportação/exclusão | Concluído | Implementar processamento automatizado seguro |
| Incidentes | Falta de workflow | Alta | Criadas Functions e documentação de resposta | Concluído | Runbooks por tipo de incidente |
| Chatbot | Prompt injection e pedidos internos | Alta | Intent `prompt_injection_attempt` e resposta segura | Concluído | Moderação externa quando IA real for ativada |
| Sessão | Estado local sensível após logout | Média | `clearUserSessionState()` limpa listeners e caches locais | Concluído | Revogação de sessão via custom claims futura |

## Auditoria v2.2-Production

- Ambientes local/staging/production documentados em `ENVIRONMENTS.md`.
- App Check em fases controladas documentado em `APP_CHECK.md`.
- Coleções operacionais (`lgpdRequests`, `systemBackups`, `deployments`, `transactionalEmails`) são backend-only pelas Firestore Rules globais deny-by-default.
- Admin Geral usa Functions protegidas por `ADMIN_EMAILS` e rate limit.
- Telegram, e-mail e pagamento permanecem com secrets somente em Functions.
- Build continua sem source maps publicados e com scan de `dist/`.
