# HabitFlow v6.17.5 — auditoria global de formulários

Auditoria estática realizada em 25/08/2026 sobre todas as views Razor e actions com
`POST`, `PUT`, `PATCH` ou `DELETE`. **Proteção transversal concluída:** toda requisição
mutacional MVC recebe antiforgery por padrão; webhooks assinados são as únicas exceções
explícitas. O script global cobre formulários mutacionais, associa erros aos campos,
move o foco, informa `aria-invalid`/`aria-required`, bloqueia envio duplo e mostra estado
de carregamento. IDs de recursos permanecem somente em rotas, valores ocultos derivados
do contexto ou opções reais carregadas pelo servidor; serviços continuam responsáveis
por validar usuário e tenant.

## Matriz auditada

| Área / formulário | Status v6.17.5 | Identidade e observação |
|---|---|---|
| Login, cadastro | Padronizado globalmente | usuário autenticado pela sessão; sem ID técnico |
| Recuperação e redefinição de senha | Padronizado globalmente | token opaco derivado do link, nunca solicitado como GUID |
| Hábitos (criar, editar, ciclo e conclusão) | Auditado | habit ID somente na rota; domínio valida proprietário/tenant |
| Lembretes | Auditado | hábito escolhido por nome/contexto; reminder ID somente na rota |
| Meu Dia | Auditado | ações contextuais; habit ID somente na rota |
| Metas semanais e revisão | Auditado | usuário e período derivados do contexto/rota |
| Objetivos | **Corrigido** | view model próprio, allowlist de indicador, limites e intervalo de datas; hábitos por nome em select |
| Desafios e conquistas | Auditado | desafio selecionado por card; usuário vem da sessão |
| Biblioteca de hábitos | Auditado | template/coleção exibidos por nome; IDs ocultos são seleção/contexto e validados no serviço |
| Planos e billing | Auditado | plano escolhido pelo nome comercial/código permitido; cliente vem da sessão |
| Suporte | Padronizado globalmente | ticket ID somente na rota; acesso filtrado por cliente/usuário |
| Feedback do assistente | Padronizado globalmente | conversa e usuário derivados da sessão |
| Configurações administrativas | Padronizado globalmente | autorização administrativa; IDs de configuração ocultos/contextuais |
| Usuários e convites | Auditado | usuários exibidos por nome/e-mail; IDs apenas nas rotas; role é seleção permitida |
| Permissões, planos e feature flags | Auditado | controles por descrição/código permitido; actions administrativas autorizadas |
| Configurações de tenant | Auditado | tenant vem do claim autenticado, nunca de input visível |
| Privacidade e LGPD | Padronizado globalmente | titular vem da sessão; solicitações administrativas usam rota e autorização |
| Superadmin, clientes, logs, exportações e documentos legais | Padronizado globalmente | recursos escolhidos por listas/cards; IDs somente nas rotas |
| MFA, notificações push e preferências | Padronizado globalmente | sessão/dispositivo derivado de opção renderizada e validada |

## IDs visíveis

A busca por inputs com `Id`, `UserId`, `ClientId`, `TenantId`, `HabitId`, `PlanId`,
`RoleId`, `FeatureId`, `ReminderId` e `TemplateId` não encontrou campo de texto destinado
à digitação de GUID/chave estrangeira. Os casos ocultos encontrados representam tokens
de idempotência ou entidades previamente carregadas, e os relacionamentos editáveis usam
selects com rótulos humanos. O objetivo editado não envia seu ID no corpo: ele é obtido da rota.

## Segurança e pendências

Não foi necessária migration. A validação global de antiforgery reduz o risco de uma nova
action MVC ser adicionada sem proteção. Webhooks Mercado Pago mantêm exceção explícita e
validação própria de provedor. A revisão de autorização/tenant confirmou o padrão de obter
`client_id` e usuário do contexto autenticado e passar ambos aos services/repositories.
Testes de integração com PostgreSQL e Playwright autenticado continuam dependendo da
infraestrutura externa documentada no projeto; não se deve substituir esses fluxos por mocks
em uma validação de release.
