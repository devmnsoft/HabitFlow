# HabitFlow v5.8 — SuperAdmin, CPF/CNPJ, Billing e Entitlements

Esta documentação cobre a evolução SaaS v5.8-SuperAdmin-ClientCpfCnpj-BillingEntitlements.

- Clientes podem ser Pessoa Física (CPF) ou Pessoa Jurídica (CNPJ), com documento normalizado apenas com números no backend.
- O perfil SuperAdmin acessa `/superadmin` e visualiza clientes, planos, assinaturas, pagamentos, inadimplência, auditoria e diagnóstico.
- Pagamentos Pix/Boleto via Mercado Pago são preparados para confirmação segura por backend/webhook/admin; retorno do navegador não libera Premium.
- Clientes inadimplentes mantêm login e recursos Free; benefícios Premium/Enterprise podem ser bloqueados e reativados sem apagar dados.
- Todas as tabelas e validações permanecem no schema explícito `habitflow`.
- Secrets devem ficar fora do Git em configuração segura de ambiente.
