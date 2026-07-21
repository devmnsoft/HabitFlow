## v4.7-PremiumTemplate-UX-ProductExperience
- Template premium do HabitFlow
- Novo layout principal responsivo
- Design system em CSS
- Componentes Razor reutilizáveis
- Landing page comercial
- Login/cadastro refinados
- Dashboard do usuário redesenhado
- Cards de hábitos premium
- Páginas de progresso e relatórios melhoradas
- Perfil, suporte e LGPD refinados
- Admin Operacional com visual executivo
- Microinterações em JavaScript Vanilla
- Melhorias mobile first
- Acessibilidade revisada
- Documentação DESIGN_SYSTEM.md
- Documentação UX_REVIEW_V4_7.md


## v4.5-DatabaseSchemaHardening-ProductionEvolution
- Padronização obrigatória do schema habitflow
- Todas as queries Dapper revisadas com habitflow.nome_tabela
- script_completo.sql reforçado para não criar tabelas em public
- script_completo_dev.sql revisado
- Migrations revisadas
- Migration 015_schema_hardening.sql
- validate_schema_habitflow.sql
- Scripts Windows/BAT de validação de schema
- DbNames com constantes de tabelas
- Diagnóstico de banco no Admin Operacional
- /health/db detalhado
- Backup por schema habitflow documentado
- Testes de schema e SQL
- Documentação DATABASE_SCHEMA_CONVENTIONS.md


## v4.4-WindowsIIS-Production-NoDocker
- Operação sem Docker formalizada.
- Scripts Windows para validação de ambiente, PostgreSQL, backup/restore, publicação IIS, rollback e smoke tests.
- Health checks /health, /health/db e /health/version.
- Diagnóstico Admin em Sistema > Ambiente.
- Migration 014 com habitflow.deployment_events.
- Documentação Windows/IIS sem Docker ampliada.

## v4.0-Rewrite-AspNetCore10
- Reescrita completa do HabitFlow
- ASP.NET Core 10
- DDD
- Clean Architecture
- Clean Code
- Dapper
- PostgreSQL
- Autenticação própria
- Dashboard
- Hábitos
- Progresso
- Perfil
- Suporte
- Chatbot por regras
- Admin Geral
- Auditoria
- Telegram backend
- WhatsApp configurável
- LGPD
- Docker
- Windows/IIS sem Docker
- CI dotnet
- Documentação completa
- Legado Firebase preservado

## v4.1-CodeQuality-SqlComplete
- Revisão completa da solução ASP.NET Core 10
- Separação de classes em arquivos próprios
- Reorganização Domain/Application/Infrastructure/Web
- Program.cs limpo com extension methods
- Controllers revisados com logger e try/catch
- Services revisados com logger e Result<T>
- Repositories Dapper revisados
- Mapeamento snake_case/PascalCase validado
- Middleware global de exceções revisado
- Auditoria reforçada
- script_completo.sql criado
- script_completo_dev.sql criado
- Migrations sincronizadas com script completo
- Testes ampliados
- Documentação atualizada

## v4.2-UserExperience-HabitRecurrence-Reports
- Recorrência de hábitos
- Dias personalizados da semana
- Meta semanal
- Horário de lembrete
- Observações no hábito
- Dashboard diário melhorado
- Onboarding com sugestões rápidas
- Página de detalhe do hábito
- Calendário mensal do hábito
- Notificações internas
- Relatórios pessoais semanais e mensais
- Exportação CSV pessoal
- Progresso melhorado
- Filtros por categoria/status
- UX mobile first refinada
- Novas migrations
- script_completo.sql atualizado
- Testes de recorrência, notificações e relatórios

## v4.3-AdminOperacional-Metrics-LGPD-Support
- Admin operacional completo
- Dashboard administrativo executivo
- Gestão avançada de usuários
- Bloqueio/desbloqueio/suspensão
- Gestão de risco
- Gestão manual de plano
- Notas administrativas
- Suporte admin
- LGPD admin
- Logs do sistema
- Auditoria administrativa
- Métricas globais
- Funil Premium
- Leads Premium
- Financeiro inicial
- Exportações CSV seguras
- Telegram para ações críticas
- AccountStatusMiddleware
- Migration 013
- script_completo.sql atualizado
- Testes administrativos

## v4.6-PremiumPayments-BillingAutomation
- Planos reais Free/Premium
- Tela de planos
- Área Minha Assinatura
- Checkout Mercado Pago
- Webhook Mercado Pago
- Ativação automática Premium por pagamento aprovado
- Histórico de transações
- Assinaturas
- Payment audit logs
- Admin financeiro real
- MRR/ARR estimado
- Notificações internas de pagamento preparadas
- Telegram para eventos financeiros documentado
- Migration 016
- script_completo.sql atualizado
- Documentação Premium/Pagamentos/Webhooks
- Testes de billing

## v4.9-GuidedJourney-PremiumUX-HabitLibrary
- Explicação simples do que é o HabitFlow
- Home reescrita com foco em clareza e conversão
- Template premium refinado
- Jornada do cliente simplificada
- Biblioteca de hábitos prontos
- Objetivos: saúde, estudos, produtividade, bem-estar e organização
- Onboarding guiado por objetivo
- Criação de hábito a partir de template
- Dashboard com próximo passo
- Planos com explicação mais simples
- Microcopy humanizada
- Admin da biblioteca de hábitos
- Métricas de jornada
- Migration 017
- script_completo.sql atualizado
- Documentação da jornada e biblioteca
