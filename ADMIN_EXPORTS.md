# Exportações Administrativas

## Exportações disponíveis

- `exportAdminUsersCsv`
- `exportPremiumLeadsCsv`
- `exportSupportTicketsCsv`
- `exportLgpdRequestsCsv`
- `exportSecurityEventsCsv`

## Proteção contra CSV injection

Células iniciadas por `=`, `+`, `-` ou `@` são prefixadas com apóstrofo antes do download.

## Campos sensíveis

Não exportar tokens, secrets, payloads completos, credenciais de pagamento, dados de cartão, chaves de IA ou Telegram.

## LGPD

Cada exportação é auditada e limitada. Exportações devem ser usadas apenas para operação legítima, suporte, segurança e obrigações legais.
