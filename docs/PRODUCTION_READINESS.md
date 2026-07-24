# PRODUCTION_READINESS

Versão: v6.1-DeepAudit-OperationalCompleteness-ProductionReadiness.

## Funcional
- Autenticação por cookie com políticas Admin e SuperAdmin.
- Schema explícito habitflow e Dapper.
- Migrations 001-029 encadeadas em database/migrate.sql.
- SuperAdmin possui telas operacionais para planos, assinaturas, pagamentos, inadimplentes, auditoria e system health.
- Ações sensíveis exigem motivo, antiforgery e registram auditoria.

## Parcial / riscos restantes
- Mercado Pago segue sem envio de dados sensíveis e depende de configuração externa real.
- Validação PostgreSQL local depende de psql e banco disponível no ambiente.
- Testes funcionais MVC dependem de SDK .NET instalado.

## Correções v6.1
- DI de IAdminBillingRepository corrigida para AdminBillingRepository.
- Migration 029 criada para fechamento operacional.
- schema_migrations preparado e exibido no System Health.
- BillingCommunicationJob processa faturas e cria comunicações internas idempotentes.
- Scripts QA adicionados para banco, placeholders, Simple.cshtml, links e assets.

## Checklist
- Rodar dotnet clean/restore/build/test/format.
- Rodar scripts/qa/*.ps1.
- Aplicar database/migrate.sql e validar validate_schema_habitflow.sql.
- Criar primeiro SuperAdmin com scripts/admin/create-superadmin.ps1.
