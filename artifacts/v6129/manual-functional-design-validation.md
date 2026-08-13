# Validação funcional e de design manual — v6.12.9

> Validação honesta: o ambiente não possui .NET SDK nem uma instância PostgreSQL autenticada. As rotas não foram abertas; a matriz registra revisão estática, não teste de runtime. Nenhum teste novo foi criado.

| Rota | Ação revisada | Resultado | Erro encontrado | Correção aplicada | Pendência real |
|---|---|---|---|---|---|
| `/onboarding` | Iniciar, escolher foco, pular | Revisão estática concluída | Criação ocorria antes de uma revisão explícita | Fluxo agora leva à customização e informa que nada foi criado | Abrir autenticado com PostgreSQL |
| `/dashboard` | Retorno após criação | Não aberta | Nenhum na revisão | Comportamento preservado | Runtime |
| `/my-day` | Destino ao pular | Revisão estática | Rota de skip ausente | POST antiforgery e feedback amigável | Runtime |
| `/habits` | Listagem | Não aberta | — | Sem alteração funcional | Runtime |
| `/habits/create` | Criação própria | Não aberta | — | Sem alteração funcional | Runtime |
| `/habits/{id}` | Detalhe | Não aberta | — | Sem alteração funcional | Runtime com dados |
| `/goals` | Listagem | Não aberta | — | Sem alteração funcional | Runtime |
| `/goals/{id}` | Detalhe | Não aberta | — | Sem alteração funcional | Runtime com dados |
| `/habit-library` | Filtrar foco, categoria, dificuldade, tempo, frequência, plano e favoritos | Revisão estática + sintaxe JS | Só havia navegação por objetivo | Grade pesquisável, contagem e estado vazio | Runtime e dados publicados |
| `/habit-library/templates/{id}` | Ver detalhe | Revisão de rota | Apenas rota singular existia | Alias plural adicionado | Runtime |
| `/habit-library/templates/{id}/customize` | Personalizar antes de criar | Revisão estática | Onboarding criava diretamente | Customização recebe origem do onboarding | Runtime autenticado |
| `/reminders` | Listagem | Não aberta | — | Sem alteração funcional | Runtime |
| `/notifications` | Listagem e ações | Não aberta | — | Sem alteração funcional | Runtime |
| `/reports` | Relatórios/exportação | Não aberta | — | Sem alteração funcional | Runtime |
| `/plans` | Gratuito/Ritmo e preços | Revisão estática apenas | — | Nenhuma regra comercial alterada | Abrir e validar checkout sandbox |
| `/account/plan/usage` | Plano, limites, recursos reais | Revisão estática | — | Acabamento compartilhado; serviço existente preservado | Runtime com catálogo |
| `/account/privacy` | Solicitações LGPD | Não aberta | — | Sem alteração funcional | Runtime |
| `/profile` | Perfil | Não aberta | — | Sem alteração funcional | Runtime |
| `/profile/accessibility` | Contraste, fonte e movimento | Revisão estática | — | Persistência existente preservada e acabamento compartilhado | Runtime autenticado |

## Viewports solicitados

`1440x900`, `1366x768`, `1280x720`, `1024x768`, `768x1024`, `430x932`, `390x844`, `360x800` e `320x568`: regras responsivas foram revisadas estaticamente (3, 2 e 1 coluna; CTAs empilhados; área segura inferior), mas **não foram validadas visualmente** porque a aplicação não pôde ser executada sem .NET SDK. Nenhuma captura foi gerada pela mesma limitação.
