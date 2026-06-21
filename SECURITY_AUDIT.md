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
