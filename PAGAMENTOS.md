# Pagamentos no HabitFlow

## Visão geral
A versão 1.6 cria a base real de monetização Premium usando Firebase Functions. O frontend inicia o checkout chamando `createCheckoutSession`; o plano só é atualizado pelo backend após webhook ou ação administrativa segura.

## Planos
- Premium Mensal: R$ 14,90/mês.
- Premium Anual: R$ 99/ano.

## Gateway principal e alternativa
Mercado Pago é o gateway preferencial para o Brasil. Stripe fica preparado como alternativa futura por `PAYMENT_PROVIDER=stripe`.

## Functions
- `createCheckoutSession`: callable autenticada que valida `planType`, busca o perfil e retorna `checkoutUrl`, `provider` e `mode`.
- `paymentWebhook`: HTTPS para receber eventos, sanitizar metadados, registrar `billingEvents` e atualizar assinatura quando possível.
- `adminSetUserPlan`: callable administrativa que valida e-mail admin no backend, altera plano e registra auditoria.

## Variáveis de ambiente
Use `functions/.env.example` como base. Nunca versione credenciais reais. Em produção, prefira Firebase Functions config/secrets.

## Como testar em sandbox/mock
Sem tokens configurados, `createCheckoutSession` retorna `mode: mock` e uma URL interna com `?payment=pending`. Isso prova a integração sem criar cobrança real.

## Webhooks e validação
Configure o gateway para chamar `paymentWebhook?provider=mercadopago` ou `paymentWebhook?provider=stripe`. A versão 1.6 contém validação mínima e placeholder para assinatura; antes de produção, implemente a validação oficial do gateway escolhido.

## Atualização do plano no Firestore
Assinaturas ficam em `users/{userId}/billing/subscription`. Ao ativar Premium, o backend atualiza `users/{userId}/profile/main.plan` e `planStatus`. Ao cancelar/inativar, volta para free sem apagar hábitos excedentes; apenas bloqueia novas criações acima do limite gratuito.

## Cancelamento futuro
A versão 1.6 não possui portal de assinatura nem cancelamento automático completo. Isso fica para v1.7+.

## Limitações da versão 1.6
- Checkout real ainda depende de credenciais e validação sandbox.
- Webhook não busca detalhes reais no gateway.
- Reembolso, portal de assinatura e e-mails transacionais ainda pendentes.
