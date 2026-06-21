# E-mails Transacionais

Provedores planejados: Resend, SendGrid ou Firebase Extension Trigger Email.

## Configuração backend
- `EMAIL_PROVIDER=future|resend|sendgrid|firebase_extension`
- `EMAIL_FROM="HabitFlow <noreply@habitflow.app>"`
- `RESEND_API_KEY=`
- `SENDGRID_API_KEY=`

As chaves nunca ficam no frontend. A Function `sendTransactionalEmail` registra `email_simulated` quando não há provedor configurado.

## Templates
Pasta: `functions/emailTemplates/`.
- welcome
- premium_interest
- support_ticket_created
- support_ticket_updated
- lgpd_request_created
- lgpd_request_completed
- security_alert_admin
- payment_success_future
- payment_failed_future
