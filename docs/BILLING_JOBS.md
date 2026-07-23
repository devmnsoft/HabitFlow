# Billing jobs

`BillingStatusHostedService` é opcional e lê `BillingJobs:Enabled`, `BillingJobs:IntervalMinutes` e `BillingJobs:GracePeriodDays`. O padrão é desativado para desenvolvimento e intervalo de 360 minutos.

`BillingStatusJob` marca faturas vencidas, aplica `PastDue` e bloqueia apenas benefícios pagos após o período de graça. Login e recursos Free continuam disponíveis.
