# Auditoria v6.6.3 — compilação e progresso

Data: 2026-07-28. Branch: `fix/compile-progress-entitlement-sync-v663`.

## Inventário inicial

Os contratos `ProgressCalendarModels.cs` e `ProgressCalendarRows.cs`, os serviços `ProgressCalendarService.cs` e `UserTimeZoneService.cs`, o repositório e as três Views de progresso estavam presentes e rastreados. Não havia arquivos não rastreados nem arquivos `obj` rastreados. A interface estava indevidamente no arquivo de Row DTOs e foi movida para `Abstractions/Progress`.

## Diagnóstico e recuperação

O primeiro erro conhecido reproduzido na máquina real era CS0173: o condicional combinava `null` e `DateOnly` com inferência por `var`. Ele foi corrigido com `DateOnly?` explícito; CS0246/CS0234 no Application, dependentes e Razor eram erros em cascata. Os caches gerados não foram editados e há script para removê-los com segurança.

## Execução neste ambiente

`dotnet --info` falhou com `dotnet: command not found`. Por isso clean, restore, builds por projeto, build da solução, testes, format e Razor publish não puderam ser executados localmente. A CI foi ordenada para executar essas etapas com .NET 10 e PostgreSQL real. Não se declara aprovação local de build, Razor, PostgreSQL ou Playwright.
