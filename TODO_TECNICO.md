## v6.1.1 CPF/CNPJ

- Acompanhar evolução visual do SuperAdmin para filtros avançados PF/PJ, pagamento e benefícios.
- Expandir testes integrados com PostgreSQL real para o fluxo completo de cadastro público.


## v6.1 riscos técnicos restantes
- Executar validação em ambiente com SDK .NET e PostgreSQL reais.
- Conectar Mercado Pago real somente com secrets fora do Git.
- Expandir testes funcionais com massa autenticada por perfil quando ambiente permitir.


## v5.8-SuperAdmin-ClientCpfCnpj-BillingEntitlements
- Cadastro de cliente com Pessoa Física/Pessoa Jurídica.
- CPF/CNPJ com validação real, máscara visual e documento normalizado/único.
- Perfil SuperAdmin e área `/superadmin` para visão global de clientes, planos, assinaturas, pagamentos e inadimplência.
- Preparação de Pix/Boleto Mercado Pago sem tokens reais e sem processamento de pagamento no frontend.
- EntitlementService para bloquear benefícios pagos mantendo acesso Free e dados do cliente.
- Auditoria SuperAdmin, notificações de cobrança/bloqueio e exportações CSV protegidas contra CSV injection.
- Projeto continua executando sem Docker na porta 5097; scripts de banco seguem no schema `habitflow`.


## v5.7-DapperDateTimeHandlers-PublicPlans-HabitLibraryBootstrap
- Corrigido suporte Dapper para DateOnly
- Corrigido suporte Dapper para TimeOnly
- Corrigido erro nos relatórios semanais/mensais
- Corrigido erro ao criar hábito com ReminderTime
- Página /plans liberada para visitantes
- Checkout/Billing continuam protegidos
- Criadas tabelas habitflow.habit_objectives e habitflow.habit_templates
- Seed completo da biblioteca de hábitos
- Fallback em memória para a Habit Library
- Adicionar hábito da biblioteca funciona com fallback
- script_completo.sql atualizado
- validate_schema_habitflow.sql atualizado
- favicon.svg criado
- Testes de DateOnly/TimeOnly, Plans e Habit Library

## v5.6.1-ClientBuildFix-FeedbackStabilization
- Validar manualmente o fluxo completo de clientes em ambiente com SDK .NET e PostgreSQL disponíveis.
- Capturar evidências visuais mobile/desktop após execução local sem Docker.
- Confirmar que publish/windows permanece fora do Git após publicação Release.


## v5.3 pendências técnicas
- Validar manualmente PostgreSQL 28P01/3D000/42P01 em ambiente com banco real.
- Capturar prints após execução local com SDK .NET disponível.


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

## v5.1 Premium Visual QA

- Validar visualmente em Windows/IIS após disponibilização da logo oficial MNSOFT.
- Avaliar persistência futura do tour guiado no banco para usuários autenticados.


## v4.5 concluído - DatabaseSchemaHardening
- Validar periodicamente que tabelas HabitFlow não existem em public.
- Antes de migrar bases antigas, revisar manualmente conflitos em public; não mover/apagar sem backup.


## v4.4-WindowsIIS-Production-NoDocker
- Operação sem Docker formalizada.
- Scripts Windows para validação de ambiente, PostgreSQL, backup/restore, publicação IIS, rollback e smoke tests.
- Health checks /health, /health/db e /health/version.
- Diagnóstico Admin em Sistema > Ambiente.
- Migration 014 com habitflow.deployment_events.
- Documentação Windows/IIS sem Docker ampliada.

## TODO técnico — pós 2.0-Security

- Executar build quando dependências estiverem disponíveis no registry.
- Testar Google Login, Firestore, Functions, Admin, Chatbot e PWA no preview.
- Criar scripts seguros para set/revoke custom claims.
- Revisar CSP em navegador real e ajustar domínios mínimos.

# TODO Técnico do HabitFlow

- Implementar backend com Firebase Functions.
- Implementar checkout real.
- Implementar webhook.
- Implementar painel admin global.
- Implementar notificações.
- Implementar relatórios PDF.
- Implementar exclusão completa de conta.
- Implementar exportação de dados LGPD.
- Implementar exclusão definitiva de dados LGPD.
- Implementar testes automatizados.

## v1.7 — Observabilidade e retenção

- Implementar limpeza automática de logs antigos via scheduled function respeitando `LOG_RETENTION_DAYS = 90` e `ERROR_LOG_RETENTION_DAYS = 180`.
- Implementar exportação de logs administrativos.
- Implementar agrupamento de bugs por assinatura de erro (`errorFingerprint`).

## v1.7.1 — Estabilização pós-console
- Validar em produção a migração completa para `users/{uid}/usageEvents/{eventId}`.
- Testar erro `auth/unauthorized-domain` em domínio não autorizado e confirmar mensagem amigável.
- Testar `sendTestTelegramAlert` pelo Admin Geral após configurar `functions/.env` sem versionar token.
- Simular `frontend_error` e conferir visibilidade em Admin Geral via `systemAuditLogs`.

## v1.8 concluída / próximos refinamentos
- Expandir cobertura automatizada de fluxos UI do chatbot.
- Criar testes unitários para normalização de WhatsApp nas Functions.
- Evoluir Admin Geral com paginação e exportação segura.

## v1.9 concluída / próximos refinamentos
- Conectar provedor real de IA somente no backend.
- Evoluir rate limit por minuto em `users/{uid}/rateLimits/chatbot`.
- Criar tela dedicada para detalhes e respostas de tickets.
- Adicionar testes automatizados de callable Functions com Firebase Emulator.

## Pós v1.9.1

- Automatizar testes end-to-end de queda/restauração de Functions.
- Validar domínio próprio em `APP_ALLOWED_ORIGINS` antes do go-live.
- Criar painel histórico para taxa de flush da fila local.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## v2.2-Production — Pendências operacionais externas
- Definir domínio final e configurar DNS/SSL.
- Criar bucket Cloud Storage de backup.
- Escolher provedor de e-mail e configurar secret backend.
- Validar App Check em monitoramento antes de enforcement.
- Configurar Mercado Pago sandbox e webhook.


## v2.3.1-Hotfix — Callable Functions

- Use `assets/js/functions-client.js` e `callFunction()` para Functions internas.
- Não chame `cloudfunctions.net` com `fetch` no frontend para Functions internas.
- `getPublicSystemSettings`, `logSystemEvent`, `healthCheck` e `sendTestTelegramAlert` precisam estar deployadas como callable/onCall.
- Deploy recomendado:

```bash
cd functions
npm install
firebase deploy --only functions:getPublicSystemSettings
firebase deploy --only functions:logSystemEvent
firebase deploy --only functions:healthCheck
firebase deploy --only functions:sendTestTelegramAlert
firebase deploy --only functions
cd ..
firebase deploy --only hosting
```

## v2.3.2 pendências operacionais
- Deployar Functions callable e Hosting juntos.
- Validar console limpo em `http://localhost:5177` após login, dashboard, suporte, chatbot e Admin Geral.
- Conferir Diagnóstico Técnico como Admin Geral e confirmar que usuário comum não visualiza a seção.

## v2.3.3-Hotfix — pendência de ambiente de build

- `npm run build` foi executado após a resolução do hotfix callable, mas o ambiente local não possui o binário `vite` instalado em `node_modules`.
- Erro retornado: `sh: 1: vite: not found`.
- Pacote afetado: `vite` (devDependency do projeto).
- Classificação: falha de ambiente/dependências, não falha de código validada por `node --check functions/index.js`, `npm test` e `npm run security:scan`.
- Não contornar com alteração de código; restaurar dependências pelo registry autorizado antes do próximo build.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.

## v2.3.5 pós-deploy
- Executar `firebase functions:list` e registrar evidência em auditoria.
- Executar `firebase deploy --only functions` e `firebase deploy --only hosting` no ambiente autenticado.
- Revalidar console em `http://localhost:5177` após limpar service worker/cache.

## v2.4 PRD pendências
- Configurar domínio final no Firebase Auth, App Check, CSP e APP_ALLOWED_ORIGINS.
- Executar smoke test PRD e atualizar PRD_DEPLOY_LOG.md.
- Habilitar Force HTTPS no IIS após certificado válido.

## v2.4.2-IIS-Publisher-Pro

- Validar publicação real em Windows Server com IIS URL Rewrite instalado.
- Autorizar domínio final IIS no Firebase Auth e App Check.
- Manter `publisher.config.json`, `publish/`, `dist/` e ZIPs fora do Git.

## v4.2 entregue
- Validar em ambiente com .NET SDK e PostgreSQL disponíveis.
- Evoluir badge de notificações para carregar contagem não lida via view component.

## v4.3 follow-up
- Conectar notificações internas detalhadas para respostas administrativas de suporte.
- Expandir snapshots periódicos do dashboard via job agendado.

## v4.6 limitações conhecidas
- Cancelamento real no Mercado Pago permanece como integração futura; nesta versão há preparação para cancelamento manual/admin e suporte.
- Envio de Telegram/e-mail para todos os eventos financeiros está documentado para configuração operacional e pode ser expandido por worker dedicado.

## v4.9 follow-up
- Medir uso real de cada template quando a camada analítica estiver ativa.
- Adicionar edição completa de templates no Admin se houver demanda operacional.

## v5.0 próximos passos
- Adicionar manualmente a logo oficial MNSOFT no caminho documentado.
- Executar QA visual em navegadores reais com PostgreSQL disponível.

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


## v5.5 pendências técnicas
- Evoluir dashboard para marcação de hábitos 100% AJAX.
- Persistir dicas descartadas por usuário no banco.
- Expandir notificações para pagamentos e relatórios prontos.

## v5.6 próximos passos

- Implementar tela completa de vínculo de usuários a clientes.
- Popular métricas de hábitos por cliente quando o vínculo estiver em uso.

## v5.9 hardening follow-up
- Ampliar filtros `client_id` em todos os repositories legados antes de produção.
- Adicionar testes de integração com PostgreSQL para vazamento cross-tenant.
- Revisar manualmente `026_backfill_client_id.sql` antes de rodar em produção.


## v6.0 operação SaaS
- Evoluir o BillingCommunicationJob para consultar faturas reais quando o módulo de invoices expuser o repositório agregado.
- Configurar canais Email/WhatsAppManual/TelegramAdmin após homologação comercial e LGPD.
- Adicionar smoke tests autenticados com banco PostgreSQL disponível.

## v6.1.2 follow-up
- Ampliar testes de integração WebApplicationFactory contra PostgreSQL dedicado.
- Evoluir filtros avançados do painel SuperAdmin Registration Quality.

## Pendências auditadas da v6.3
- Concluir métricas reais e sequência na resposta Ajax do Dashboard.
- Implementar processadores de lembrete/resumos com locking transacional.
- Finalizar calendário, relatórios avançados, rotinas e painel da conta no nível de aplicação.
- Ampliar testes de isolamento, CSV, scheduler, PWA e regressão mobile.

## Pós-v6.5.1 — consolidação controlada

- [ ] Migrar tokens/regras de `site.css` para módulos em incrementos visualmente comparados; manter `tokens.css`, `base.css` e `components.css` inativos até concluir a migração.
- [ ] Provisionar personas efêmeras (Gratuito, proprietário, Super Administrador) na CI para ampliar o Playwright sem armazenar secrets.
- [ ] Adicionar snapshots baselines aprovados em storage de artifacts, nunca no repositório.
