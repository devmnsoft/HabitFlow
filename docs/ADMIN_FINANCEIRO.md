# HabitFlow v4.6 - ADMIN_FINANCEIRO

A versão v4.6-PremiumPayments-BillingAutomation adiciona planos Free/Premium, assinaturas, checkout Mercado Pago e auditoria financeira usando PostgreSQL/Dapper no schema `habitflow`.

## Configuração

- `Payment:Provider`: `MercadoPago`.
- `Payment:Mode`: `Sandbox` ou `Production`.
- `Payment:PublicBaseUrl`: URL pública da aplicação, porta local padrão `5097`.
- `MercadoPago:AccessToken`: configurar por segredo de ambiente/IIS, nunca commitar token real.
- `MercadoPago:WebhookSecret`: opcional; quando configurado, o webhook valida assinatura.
- `MercadoPago:NotificationUrl`: URL pública de `/webhooks/mercadopago`.

## Segurança

O retorno do navegador em `/billing/return/*` não ativa Premium. A ativação ocorre somente no backend por webhook/admin após pagamento aprovado. Payloads são sanitizados antes de persistir e nenhum dado sensível de cartão é armazenado.

## Teste sem token

Com `MercadoPago:AccessToken` vazio, o checkout exibe "Pagamento ainda não configurado neste ambiente." sem ativar cobrança real.

## Windows/IIS sem Docker

Docker continua opcional. Em IIS, configure variáveis seguras no ambiente do Application Pool e use domínio HTTPS público para receber webhooks. Em desenvolvimento local sem URL pública, o webhook externo não chegará sem túnel temporário.
