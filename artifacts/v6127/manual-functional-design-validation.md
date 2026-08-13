# Validação funcional e visual manual — v6.12.7

> Validação honesta: foi feita inspeção estática de rotas, views, formulários, media queries e contratos. A execução web não foi possível sem o SDK .NET e sem uma sessão/infraestrutura PostgreSQL. Nenhum teste ou spec foi criado.

| Rota | Ação inspecionada | Resultado | Erro encontrado | Correção aplicada | Pendência real |
|---|---|---|---|---|---|
| `/dashboard` | KPIs, próxima ação, objetivo e uso do plano | Contratos e partials presentes | Nenhum estático | Preservado | Abrir com dados reais |
| `/my-day` | Priorizar, concluir/desfazer, adiar, pausar e restaurar | Formulários antiforgery via tag helpers e feedback existentes | Organização por períodos não respondia diretamente “agora/próximos” | Seções acionáveis, saudação, motivação, métricas, frequência e CTAs | Exercitar mutations no PostgreSQL |
| `/habits` | Buscar, filtrar categoria/status e paginação | Presente | Nenhum estático | Preservado | Validar dados reais |
| `/habits/create` | Categoria, objetivo e gate de plano | Presente | Próximo passo não oferecia vínculo | CTA de vínculo incluído | Validar limite de conta Free |
| `/habits/{id}` | Arquivar/restaurar e ações | Histórico preservado | Não havia duplicação | POST tenant-scoped, gate de plano e cópia sem lembrete/histórico | Executar duplicação real |
| `/goals` | Buscar/status/cards | Presente | Nenhum estático | Preservado | Validar todos os status |
| `/goals/{id}` | Progresso, vínculo por select, timeline e lifecycle | Presente e tenant-scoped | Nenhum estático | Preservado | Executar vínculo/desvínculo |
| `/reminders` | Estado vazio e ações | Partial e controller presentes | Nenhum estático | Preservado | Validar agendamento real |
| `/notifications` | Leitura, arquivamento e vazio | Partials e controller presentes | Nenhum estático | Preservado | Validar paginação real |
| `/plans` | Oferta mensal/anual e grandfathering | Contratos preservados | Nenhum estático | Nenhuma oferta adicionada | Validar checkout externo |
| `/account/privacy` | Ações e feedback | Controller/view presentes | Nenhum estático | Preservado | Exercitar solicitações reais |

## Viewports

As regras CSS foram inspecionadas para `1440x900`, `1366x768`, `1024x768`, `768x1024`, `430x932`, `390x844`, `360x800` e `320x568`. Em até 700 px os cards passam a duas colunas e ações ocupam toda a linha; em até 390 px CTAs vazios ocupam 100%, metadados secundários são reduzidos e os alvos continuam com no mínimo 44 px. Screenshot não foi produzido porque a aplicação não pode iniciar sem `dotnet`.
