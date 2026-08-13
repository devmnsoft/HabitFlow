# Validação de runtime — v6.13.2

## Correções verificadas estaticamente

- A listagem e o detalhe de hábitos apontam para o partial existente pelo caminho explícito.
- O badge contempla Ativo, Pausado, Arquivado, Agendado e fallback Indisponível.
- Listagem e busca de lembretes usam DTO interno, mapper e conversões UTC.
- Create, pause/resume e snooze enviam `DateTime` UTC para colunas PostgreSQL `timestamp`.
- A tela existente já oferece estado vazio, nomes abreviados dos dias, timezone legível, feedback via `TempData` e confirmação acessível baseada em `data-confirm`, sem `confirm()` inline.

## Execução local

O container não possui o SDK/runtime `dotnet` (`dotnet: command not found`). Por isso não foi possível iniciar `http://localhost:5097`, autenticar, abrir `/habits` e `/reminders`, capturar screenshot, nem executar ações persistentes contra PostgreSQL. Nenhuma rota ou ação é declarada como validada manualmente neste ambiente.

## Pendência real

Em ambiente com .NET 10 e PostgreSQL configurado, executar o build, iniciar a aplicação, autenticar e validar: `/habits`, detalhe, arquivar/restaurar, pausar/reativar, concluir/desfazer; `/reminders`, `/habits/{habitId}/reminders`, criar/listar/pausar/reativar/adiar 15 min/adiar 1 h/excluir/recarregar.
