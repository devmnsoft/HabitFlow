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
