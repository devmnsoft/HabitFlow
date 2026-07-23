# JOBS

Versão v6.0-SaaSLaunch-Onboarding-BillingCommunication-Operations.

- Usa Dapper, PostgreSQL e schema explícito `habitflow`.
- Mantém execução sem Docker e porta 5097.
- Preserva isolamento por `client_id` para Admin do cliente; SuperAdmin possui visão global controlada.
- Não armazena payload sensível de pagamento nem libera Premium por retorno de navegador.

Consulte a migration `database/migrations/028_client_onboarding.sql` para tabelas de onboarding, régua, comunicações e logs de jobs.
