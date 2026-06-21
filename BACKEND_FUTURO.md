# Backend Futuro do HabitFlow

A versão atual do HabitFlow 1.5 é frontend-only e usa Firebase Authentication e Cloud Firestore diretamente no navegador, com regras que isolam os dados por usuário.

## Funções futuras recomendadas

- Criar checkout de pagamento para planos mensal e anual.
- Confirmar webhooks de pagamento com validação de assinatura.
- Atualizar o plano do usuário (`plan` e `planStatus`) com credenciais administrativas.
- Registrar métricas globais com segurança, sem permitir escrita pública em `appMetrics`.
- Enviar e-mails para usuários que demonstraram interesse no Premium.
- Enviar notificações de retenção e lembretes.
- Exportar relatórios em PDF.
- Construir painel admin global com métricas agregadas e permissões robustas.

## Stack sugerida

- Firebase Functions.
- Mercado Pago ou Stripe.
- Firestore Admin SDK.
- Webhooks autenticados.
- Envio de e-mail por SendGrid, Resend ou Firebase Extension.

## Segurança

O frontend não deve gravar métricas globais nem alterar planos pagos em produção. Essas ações devem ser feitas por backend confiável para evitar abuso, fraude e exposição indevida de dados.

## Versão 1.6 — Implementado parcialmente
- Firebase Functions adicionadas em `functions/`.
- Checkout Premium preparado via `createCheckoutSession`.
- Webhook de pagamento preparado via `paymentWebhook`.
- Modelo de assinatura em `users/{userId}/billing/subscription`.
- Atualização de plano pelo backend em `profile/main`.
- Audit logs administrativos em `adminAuditLogs`.

## Ainda pendente
- Pagamento real em produção com credenciais Mercado Pago.
- Cancelamento automático completo.
- Portal de assinatura.
- Fluxo de reembolso.
- E-mails transacionais.
- Painel admin global completo.
