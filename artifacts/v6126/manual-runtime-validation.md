# v6.12.6 — validação manual de runtime

## Limitação do ambiente

Em 13/08/2026, o executável `dotnet` não estava instalado/disponível no `PATH`
do ambiente fornecido. Portanto, o servidor ASP.NET Core não pôde ser iniciado
em `http://localhost:5097` e **nenhuma rota abaixo é declarada como aberta ou
validada em navegador nesta execução**. A tentativa encerrou antes do runtime
com `/bin/bash: dotnet: command not found`.

## Matriz de validação pendente

| Rota | Ação planejada | Resultado | Erro encontrado | Correção aplicada | Pendência real |
|---|---|---|---|---|---|
| `/dashboard` | Abrir e inspecionar cards/overlays | Não executada | SDK ausente | Nenhuma | Validar em ambiente com .NET 10 e dados autenticados |
| `/my-day` | Abrir e operar rotina | Não executada | SDK ausente | Nenhuma | Idem |
| `/habits` | Abrir, filtrar e inspecionar responsividade | Não executada | SDK ausente | Nenhuma | Idem |
| `/habits/create` | Preencher editor, preview e salvar | Não executada | SDK ausente | Nenhuma | Idem |
| `/habits/{id}` | Abrir detalhe e ações | Não executada | SDK ausente | Nenhuma | Idem |
| `/habits/{id}/edit` | Editar e salvar | Não executada | SDK ausente | Nenhuma | Idem |
| `/goals` | Abrir cards e progresso | Não executada | SDK ausente | Nenhuma | Idem |
| `/goals/create` | Criar objetivo | Não executada | SDK ausente | Nenhuma | Idem |
| `/goals/{id}` | Abrir detalhe, vínculos e lifecycle | Não executada | SDK ausente | Nenhuma | Idem |
| `/plans` | Abrir e alternar período | Não executada | SDK ausente | Nenhuma | Idem |
| `/account/privacy` | Abrir painéis | Não executada | SDK ausente | Nenhuma | Idem |
| `/notifications` | Abrir e marcar como lida | Não executada | SDK ausente | Nenhuma | Idem |
| `/reminders` | Abrir e operar lembretes | Não executada | SDK ausente | Nenhuma | Idem |

## Viewports pendentes

Os viewports `1440x900`, `1366x768`, `1024x768`, `768x1024`, `430x932`,
`390x844`, `360x800` e `320x568` não foram declarados como validados, pois a
aplicação não pôde ser iniciada.
