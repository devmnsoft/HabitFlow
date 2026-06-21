## v2.0-security
- Auditoria de segurança inicial
- Build de produção com dist/
- Minificação de HTML/CSS/JS
- Ofuscação leve/moderada pós-build
- Source maps desativados em produção
- Firebase Hosting publicando apenas dist/
- Scanner básico de secrets
- Separação APP_ENV development/production
- App Check preparado
- Firestore Rules reforçadas
- Lógica sensível validada em Functions
- Admin Geral protegido por backend
- Chatbot com regras de segurança reforçadas
- Logger com sanitização reforçada
- Headers de segurança e CSP
- Service worker revisado
- localStorage revisado
- .gitignore e .firebaseignore reforçados
- Documentação SECURITY.md, SECURITY_CHECKLIST.md, APP_CHECK.md, CSP.md e BUILD_PRODUCTION.md

## v1.7

## v1.7.1
- Correção do erro Firestore em users/{uid}/usage/events
- Nova coleção users/{uid}/usageEvents
- Correção de meta tag mobile-web-app-capable
- Tratamento amigável para erros Firebase Auth/identitytoolkit
- Correção de promises sem catch
- handleAppError centralizado
- Captura global de erros frontend
- Reporte de bugs para Admin Geral
- Telegram real preparado para @hablitflowmns_bot
- sendTestTelegramAlert
- Reforço de Firestore Rules
- Documentação de correções e monitoramento


- Painel Admin Geral.
- Perfil de Administrador Geral.
- Logs globais em systemAuditLogs.
- Captura global de erros frontend.
- Monitoramento de actions dos usuários.
- Integração com Telegram via Firebase Functions.
- Alertas Telegram para erros, bugs e eventos importantes.
- Função de teste do Telegram.
- Dashboard administrativo com eventos recentes.
- Painel de bugs e erros.
- Filtros de logs por severidade/tipo/usuário/ambiente.
- Sanitização de metadata.
- Firestore Rules reforçadas.
- Documentação TELEGRAM_MONITORAMENTO.md.
- Documentação OBSERVABILIDADE.md.
- Preparação para retenção de logs.

# Changelog

## v1.7 Telegram real
- Bot @hablitflowmns_bot configurado
- Chat ID administrativo configurado
- Function sendTelegramAlert preparada
- Function sendTestTelegramAlert criada
- Painel Admin Geral mostra status do Telegram
- Alertas de erro e eventos importantes enviados para o Telegram
- Documentação de segurança adicionada

## v1.5

- Refatoração de arquitetura JavaScript.
- Perfil expandido.
- Arquivamento lógico de hábitos.
- Área de hábitos arquivados.
- Premium simulado.
- Controle de plano no Firestore.
- Preparação para checkout futuro.
- Métricas de uso ampliadas.
- Admin inicial melhorado.
- Consentimento de Termos e Privacidade.
- Onboarding guiado.
- Desafios futuros.
- Relatórios pessoais básicos.
- Reforço de LGPD e segurança.

## v1.4

- Área Admin inicial para usuário atual.
- Eventos simples de uso.
- Insights pessoais e ranking de hábitos.
- Estrutura visual de Premium futuro.

## v1.3

- Onboarding com sugestões de hábitos.
- Plano gratuito limitado a 5 hábitos.
- Perfil do usuário no Firestore.
- Registro de interesse no Premium.

## v1.2

- Categorias de hábitos.
- Aba Hoje, Progresso e Perfil.
- Estados vazios refinados.
- Tratamento centralizado de erros.

## v1.1

- Login e cadastro com e-mail/senha.
- Editar e excluir hábito.
- Modal de confirmação.
- PWA básico e SEO básico.

## v1.0

- Landing page comercial.
- Login com Google.
- Dashboard autenticado.
- Criar hábito.
- Marcar e desmarcar hábito feito hoje.
- Streak atual, maior streak e histórico visual dos últimos 30 dias.

## v1.6
- Estrutura Firebase Functions.
- Checkout Premium preparado.
- Mercado Pago como gateway principal.
- Stripe preparado como alternativa futura.
- Webhook de pagamento preparado.
- Modelo de assinatura no Firestore.
- Atualização automática de plano via backend.
- Audit logs administrativos.
- Regras Firestore reforçadas.
- Frontend integrado ao checkout.
- Tratamento de retorno pós-pagamento.
- Documentação PAGAMENTOS.md.

## v1.9
- Arquitetura segura para chatbot com IA via backend.
- Function askHabitFlowAssistant.
- Base de conhecimento completa do HabitFlow.
- Intents do assistente.
- Detecção de conteúdo sensível.
- Histórico controlado de conversas.
- Tickets de suporte.
- Reporte de bug pelo chatbot.
- Central de Suporte no Perfil.
- Aba Suporte no Admin Geral.
- Integração do chatbot com Telegram.
- Integração do chatbot com WhatsApp/e-mail MNSOFT.
- Métricas do chatbot.
- Documentação IA_SEGURA.md e SUPORTE.md.
- Reforço de logger e try/catch nos fluxos do assistente.

## v1.8
- Try/catch aplicado em fluxos críticos.
- Logger centralizado no frontend com `safeAsync`.
- Logger/backend auditável com status de bugs e fingerprint.
- Logs mais claros para ações, erros e bugs.
- Painel Admin Geral com saúde do sistema, bugs e ações.
- Status de bugs: novo, lido, resolvido e ignorado.
- Chatbot Assistente HabitFlow baseado em conhecimento local.
- Regras de segurança do chatbot e bloqueio de dados sensíveis.
- Reporte de bug pelo chatbot.
- Configuração de WhatsApp pelo Admin Geral.
- Botões de atendimento MNSOFT e dados institucionais públicos.
- Documentação `CHATBOT.md`, `SUPORTE_WHATSAPP.md` e `LOGGER.md`.

## v1.9.1
- Correção de CORS nas Firebase Functions HTTP com utilitário compartilhado.
- Substituição/centralização de chamadas internas por `httpsCallable` via `functions-client.js`.
- Fallback local para system settings com defaults MNSOFT.
- Proteção anti-loop no logger remoto.
- Fila local de logs pendentes com sanitização e flush.
- Deduplicação de erros repetidos no monitoramento frontend.
- Correção do fluxo `beforeinstallprompt` do PWA.
- `healthCheck` callable para diagnóstico de Functions.
- Diagnóstico de Functions no Admin Geral.
- Documentação `FUNCTIONS_CORS.md` e `PWA.md`.

## v2.1-SecurityOps
- Pipeline GitHub Actions de segurança.
- `security:scan`, `security:dist`, validação de `firestore.rules` e `firebase.json`.
- Testes básicos de segurança.
- Rate limit em Functions críticas e registro de eventos suspeitos.
- Painel de risco no Admin Geral.
- Controle de incidentes e solicitações LGPD de exportação/exclusão.
- Documentação de backup, recuperação e resposta a incidentes.
- Proteção contra prompt injection, enumeração de usuários e XSS revisado.
- Headers anti-clickjacking e limpeza segura de sessão.

## v2.2-Production
- Ambientes local/staging/production documentados.
- Preparação para domínio próprio.
- App Check em modo controlado.
- Backup Firestore operacional documentado.
- Registro de status de backups.
- LGPD operacional com exportação/exclusão controlada.
- E-mails transacionais preparados.
- E-mail de boas-vindas simulado/real.
- Monitoramento de produção no Admin Geral.
- Production readiness score.
- Pagamento Mercado Pago sandbox preparado.
- Go-live checklist.
- Post-deploy checklist.
- Páginas legais refinadas.
- Deploy controlado documentado.
- Registro de deploys.
