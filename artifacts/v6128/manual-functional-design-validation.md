# Validação funcional e de design — v6.12.8

> Nenhuma rota foi declarada como aberta/validada: o ambiente não possui SDK .NET, PostgreSQL configurado, sessão autenticada ou navegador. A revisão possível foi estática e programática.

| Rota | Ação revisada | Resultado | Erro encontrado | Correção aplicada | Pendência real |
|---|---|---|---|---|---|
| `/dashboard` | resumo, KPIs, semana e novo usuário | Revisão estática concluída | visão executiva incompleta | KPIs, gráfico acessível, streaks, recomendação e templates | abrir com dados reais |
| `/my-day` | regressão estática | preservado | nenhum nesta etapa | nenhuma | abrir autenticado |
| `/habits` | regressão estática | preservado | nenhum nesta etapa | nenhuma | abrir autenticado |
| `/habits/create` | limite/template | preservado | nenhum nesta etapa | nenhuma | validar plano Free |
| `/habits/{id}` | navegação por lembrete | rota reutilizada | nenhum nesta etapa | CTA no card de lembrete | abrir com hábito real |
| `/goals` | busca/status/ordenação | revisão estática concluída | faltava ordenação | seis opções e cards acionáveis | testar consultas reais |
| `/goals/{id}` | vínculo e próxima ação | revisão estática concluída | âncora de vínculo ausente | CTA e destino sem Guid manual | validar antiforgery em runtime |
| `/reminders` | grupos e ações | revisão estática concluída | faltavam agrupamento e adiar 1h | Hoje/Próximos/Ativos/Pausados, próximo disparo e ações | validar timezone/disparo |
| `/notifications` | inbox e POST | preservado | nenhum nesta etapa | nenhuma | abrir com lista vazia/cheia |
| `/reports` | semanal/mensal/CSV/print | revisão estática concluída | planejados não eram exibidos | métrica honesta e insight acionável | conferir dados e impressão |
| `/habit-library` | filtros/customização | preservado | nenhum nesta etapa | integração via dashboard | validar limite/duplicidade |
| `/plans` | integridade comercial | revisão estática | nenhum nesta etapa | nenhuma mudança comercial | checkout externo |
| `/account/privacy` | regressão estática | preservado | nenhum nesta etapa | nenhuma | abrir autenticado |

## Viewports

Os breakpoints e regras contra overflow foram revisados em CSS, mas **nenhum viewport foi declarado visualmente validado** pela ausência de aplicação executável/navegador autenticado: 1440×900, 1366×768, 1280×720, 1024×768, 768×1024, 430×932, 390×844, 360×800 e 320×568 permanecem pendentes de inspeção visual real.
