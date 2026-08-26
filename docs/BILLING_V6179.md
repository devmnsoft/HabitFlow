# Billing real e modo comercial seguro — v6.17.9

O checkout nasce desabilitado. Em `Payments:Enabled=false` ou com o provider `Manual`, nenhuma assinatura pendente é criada e nenhum recurso pago é liberado. A tela de planos encaminha o cliente para `comercial@mnsoft.com.br` (e para WhatsApp somente quando configurado).

## Mercado Pago

1. Configure `Payments__Enabled=true` e `Payments__Provider=MercadoPago`.
2. Forneça `MercadoPago__AccessToken` e `MercadoPago__WebhookSecret` **somente por secret store/variáveis de ambiente**.
3. Configure URLs HTTPS públicas de retorno e notificação.
4. Cadastre `/webhooks/mercadopago` no provedor.

O checkout é hospedado pelo Mercado Pago; o HabitFlow não coleta nem persiste cartão. A assinatura começa pendente e só fica ativa após consulta server-to-server causada por webhook com assinatura válida. Eventos repetidos são aceitos sem repetir o efeito, usando a chave única `(provider,event_id)`. O payload persistido passa pelo sanitizador de campos sensíveis.

## Operação manual

O modo manual é apenas um fluxo comercial: o usuário entra em contato e um administrador autorizado pode registrar uma alteração com motivo, ator e correlation ID. Ele não simula transação. A migration `078` cria o ledger de ajustes, uso e eventos estruturados, sempre com `client_id` para manter isolamento por tenant.

## Estados e acesso

`PaymentPending` não concede Premium. `Active` e `Trialing`, dentro das datas válidas, concedem os entitlements do catálogo. Cancelamentos agendados preservam acesso até o fim do ciclo pago; downgrade preserva dados e bloqueia somente novas criações acima do limite. `PastDue`, `Expired`, `Canceled` e falhas voltam aos limites efetivos definidos pelo catálogo após a tolerância aplicável.
