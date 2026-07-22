## v5.6.2-EnumPersistenceFix-PublicNavigation-FooterUX
- Corrigida persistência de enums como texto no Dapper
- Corrigido erro 23514 em habitflow.users.account_status
- Corrigido erro 23514 em habitflow.system_audit_logs.severity
- Criado helper DbEnum
- Criado validate_enum_constraints.sql
- PostgresErrorHelper reconhece CHECK constraint violation
- Cadastro de usuário estabilizado
- Auditoria resiliente
- Navbar pública reorganizada
- Link Planos visível para visitantes
- Navbar logada separada da pública
- Ícone de Ajuda corrigido com SVG
- Botões Entrar/Começar grátis realinhados
- Footer premium redesenhado
- MNSOFT badge compacta refinada
- Testes de enum, navbar e footer


## v5.6.1-ClientBuildFix-FeedbackStabilization
- Corrigido erro CS0509 em UpdateClientRequest
- Criada base ClientRequestBase para Create/Update
- Corrigidos namespaces dos DTOs de clientes
- Corrigidas views Razor de clientes
- Corrigido AdminClientsController
- Garantido registro de ClientService e IClientRepository
- Estabilizado cadastro de cliente
- Feedback de ações do módulo clientes revisado
- Documentada limpeza de bin/obj para Razor Source Generator
- Testes de compilação e clientes ajustados


## v5.3-AuthUX-DatabaseMessages-ContextualPremiumTemplate
- Cadastro com confirmação de senha
- Validação backend de senha e confirmação
- Campos de senha padronizados
- Botão de mostrar/ocultar senha com olhinho
- Senha oculta por padrão
- Mensagens visuais premium com alto contraste
- Mensagens específicas para erro de banco
- Separação de mensagem pública e mensagem técnica
- Diagnóstico PostgreSQL 28P01 melhorado
- AuthController usando DatabaseError
- AuditService sem cascata em erro de banco
- Ilustrações contextuais por página
- Login e cadastro redesenhados
- Documentação de mensagens de banco
- Documentação de UX de senha
- Testes de autenticação e mensagens


## v5.2-DatabaseConnectionFix-PremiumDemo-UXNavigation
- Correção amigável para erro PostgreSQL 28P01
- Suporte a appsettings.Development.local.json
- Scripts de validação de conexão PostgreSQL
- /health/db com diagnóstico de senha inválida
- AuditService resiliente a falha de banco
- Botão Ver demonstração corrigido
- Página /demo funcional sem banco
- Demo interativa com JavaScript Vanilla
- Navbar revisada com ícones e descrições
- Menu visitante e menu logado separados
- Footer corrigido
- Bloco MNSOFT sem fallback feio
- Ilustrações SVG inline
- Biblioteca de ícones SVG
- Home mais premium e vendável
- Central de Ajuda mais clara
- Manual rápido por tela
- Checklist de primeiros passos
- Tour guiado funcional
- Scripts de validação de assets, links e placeholders
- Template refinado para usuário comum

## vNext-DatabaseBootstrap-ResilientPostgres
- Correção do erro PostgreSQL 3D000 para banco habitflow inexistente.
- Scripts para criar banco PostgreSQL habitflow, aplicar schema completo, seed dev e validar tabelas obrigatórias.
- seed_dev.sql com admin local de desenvolvimento.
- Health check detalhado passa a retornar mensagem amigável quando banco está ausente.
- Autenticação evita cascata de auditoria quando a conexão com o banco não abre.
- Auditoria resiliente a falhas de conexão do PostgreSQL.
- Documentação de diagnóstico do banco ausente.

## v5.1-PremiumVisualQA-HelpCenter-GuidedExperience
- Auditoria visual do template
- Correção de imagens faltantes
- Fallback seguro para logo oficial MNSOFT
- Biblioteca de ícones SVG inline
- Ilustrações SVG premium sem binários
- CSS reorganizado e documentado
- Home mais didática e vendável
- Central de Ajuda
- Manual do Usuário
- Tour guiado do primeiro uso
- Checklist de primeiros passos
- Dashboard mais humano
- Biblioteca de hábitos mais visual
- Planos com copy comercial e FAQ
- Suporte com mais confiança MNSOFT
- Scripts de validação de assets e placeholders
- Documentação de UX, vendas e manual

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

## v5.0-AccessibilityContrast-OfficialMNSOFTLogo-PremiumUI
- Auditoria de contraste visual
- Design tokens consolidados
- Correção de legibilidade em cards, fundos, badges, links e botões
- Modo alto contraste
- Preferência de fonte maior
- Preferência para reduzir animações
- Tabela habitflow.user_ui_preferences
- Tela de preferências de visualização
- Logo oficial MNSOFT preservada e preparada no template
- Documentação de uso da logo oficial
- Checklist visual de QA
- Script de verificação de tokens de cor
- Template premium refinado com foco em legibilidade
- Mobile e acessibilidade revisados

## v5.4-UserSafeErrors-HabitLibraryFix-PremiumFooter-HeroContext
- Correção do uso inválido de [Compare]
- Cadastro com ViewModel apropriado
- Ocultação de erros técnicos para usuário final
- Diagnóstico de banco restrito a admin/dev
- Mensagens amigáveis para falhas de infraestrutura
- Correção funcional da Habit Library
- Fallback útil para objetivos/hábitos
- Nova hero illustration contextual ao software
- Home mais coerente com o negócio
- Rodapé premium redesenhado
- Assinatura MNSOFT compacta com ícone SVG
- Mais ícones contextuais
- Ajuda contextual por página
- CSS reorganizado
- Testes de segurança visual e funcional

## v5.5-PremiumPopups-SmartFeedback-Engagement
- Sistema premium de toasts
- Modal global de feedback
- Modal de confirmação reutilizável
- Pop-up de erro de banco amigável
- Pop-up de conquistas
- Feedback de hábito concluído
- Feedback de limite Free
- Habit Library com fallback e retry
- Central de notificações evoluída
- Preferências de pop-ups
- Dicas contextuais
- Remoção de alert/confirm nativos
- Mensagens inline para validação de campos
- FeedbackMapper para erros técnicos
- Documentação de feedback e notificações
- Testes de pop-ups, mensagens e notificações

## v5.6-ClientRegistration-ActionMessages-MNSOFTBranding-FunctionalEvolution
- Cadastro completo de clientes
- Listagem de clientes
- Edição de clientes
- Ativar/desativar/bloquear cliente
- Detalhe do cliente
- Estrutura de vínculo cliente-usuário
- Métricas iniciais do cliente
- Auditoria de ações de cliente
- Feedback global de ações
- Toasts para ações de sucesso
- Modais para erros e confirmações
- FeedbackBridge via TempData
- Padronização de mensagens por ação
- MNSOFT brand badge refinado
- Rodapé premium reorganizado
- Documentação de clientes e feedbacks
