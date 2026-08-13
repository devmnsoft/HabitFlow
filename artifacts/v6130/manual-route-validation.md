# Validação manual de rotas — v6.13.0

A aplicação não pôde ser iniciada porque o ambiente não possui `dotnet`. Portanto, nenhuma rota abaixo é apresentada como aberta ou funcionalmente aprovada. A coluna “inspeção” registra somente a existência estática de controller/route/view observada no código.

| Rota | Ação | Status runtime | Inspeção/correção | Pendência |
|---|---|---:|---|---|
| `/dashboard` | abrir KPIs | Não executado | contrato existente | autenticar, abrir, recarregar |
| `/my-day` | concluir/desfazer | Não executado | contrato existente | validar persistência |
| `/habits` | listar/filtrar | Não executado | contrato existente | validar filtros |
| `/habits/create` | criar | Não executado | contrato existente | validar limite e feedback |
| `/habits/{id}` | detalhe | Não executado | contrato existente | usar ID real |
| `/habits/{id}/edit` | editar | Não executado | contrato existente | persistência |
| `/goals` | listar/filtrar | Não executado | contrato existente | sessão com dados |
| `/goals/create` | criar | Não executado | contrato existente | persistência |
| `/goals/{id}` | vínculo | Não executado | contrato existente | vínculo/desvínculo |
| `/habit-library` | filtrar/favoritar | Não executado | links presentes | sessão e banco |
| `/habit-library/templates/{id}` | detalhe | Não executado | rota explícita localizada | ID real |
| `/habit-library/templates/{id}/customize` | customizar | Não executado | rota explícita localizada | criação real |
| `/onboarding` | jornada | Não executado | contrato existente | conta nova |
| `/reminders` | pausar/adiar | Não executado | incluído no menu pessoal nesta entrega | banco e sessão |
| `/notifications` | ler/arquivar | Não executado | incluído no menu pessoal nesta entrega | banco e sessão |
| `/reports` | abrir/exportar | Não executado | rota explícita localizada | dados e CSV |
| `/plans` | ciclos/checkout | Não executado | contrato existente | gateway configurado |
| `/account/plan/usage` | limites | Não executado | links presentes | sessão e plano |
| `/account/privacy` | consentimentos/pedidos | Não executado | confirmação modal existente | persistência |
| `/profile` | abrir | Não executado | link presente | sessão |
| `/profile/accessibility` | salvar | Não executado | link presente | persistência e reload |

## Jornadas e viewports

As jornadas de usuário novo e usuário com dados estão **pendentes**, assim como todos os nove viewports solicitados. `artifacts/v6130/screenshots/` foi reservado, mas nenhuma imagem falsa foi gerada.
