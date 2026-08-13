# Relatório final — v6.13.2 runtime hotfix

## `_HabitStatusBadge`

- **Erro:** Razor não encontrava `_HabitStatusBadge` ao renderizar o card.
- **Causa:** o nome curto não inclui `Habits/Partials` nos locais convencionais de busca; embora o arquivo estivesse versionado, ele não era resolvível por esse nome a partir da view chamadora.
- **Correção:** o partial versionado em `Views/Habits/Partials/_HabitStatusBadge.cshtml` foi completado e card e detalhe usam `Partials/_HabitStatusBadge`; CSS acessível e responsivo adicionado.

## `HabitReminder`

- **Erro:** Dapper exigia construtor compatível para materializar o record.
- **Causa:** nomes snake_case e `timestamp`/`DateTime` não coincidiam com propriedades e parâmetros `DateTimeOffset?` do domínio.
- **Correção:** `HabitReminderRow` privado, aliases explícitos, mapper para domínio, `DateTime -> DateTimeOffset UTC` na leitura e `DateTimeOffset -> DateTime UTC` na escrita. `days_of_week` usa `array[]::integer[]`.

## Validações

- `npm run security:scan`: passou.
- `npm test`: passou (somente testes existentes; nenhum teste criado ou alterado).
- `npm audit --omit=dev`: passou, 0 vulnerabilidades.
- Oito comandos `node --check`: passaram.
- `dotnet build HabitFlow.sln --configuration Release`: não executado por ausência do comando `dotnet` no container.
- Runtime e rotas: não executados pelo mesmo bloqueio; não são declarados como validados.

## Pendências reais

Executar build e validação autenticada/persistente das rotas e ações descritas em `runtime-validation.md` em ambiente com .NET 10 e PostgreSQL. Não foram adicionados secrets, binários ou testes.
