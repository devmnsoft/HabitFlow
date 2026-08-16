# Relatório final de validação v6.13.3

- **SHA inicial:** `780b01b0a2853c734fede0183ade433858ba977b`
- **SHA final:** preenchido pelo commit Git; este documento pertence ao conteúdo do próprio commit.
- **Build/publish:** bloqueados; `dotnet` ausente (exit 127), downloads via script/APT bloqueados pelo proxy HTTP 403.
- **Migrations:** bloqueadas; `pwsh`, `psql` e PostgreSQL ausentes. Runner endurecido para escopo de lembretes e tabelas de lembretes/templates.
- **Startup, `/habits`, `/reminders`, rotas, jornadas, responsividade e planos:** não executados e não declarados aprovados.
- **Partials:**  varredura estática completa, nenhum arquivo referenciado ausente; runtime Razor pendente.
- **Dapper:** varredura estática completa; hotfix de reminders confirmado; `select *` administrativo/dynamic registrado fora das rotas prioritárias; PostgreSQL pendente.
- **npm/security:** `security:scan`, testes existentes e `npm audit --omit=dev` aprovados; oito `node --check` aprovados.
- **Bugs/correções:** runner v6132 ainda emitia relatório/banco temporário antigo e não exigia escopo dos lembretes nem tabelas reminders/templates; atualizado para v6133 com assertions explícitas.
- **Testes novos:** nenhum criado ou alterado.
- **Binários/screenshots:** nenhum fabricado ou incluído.
- **Pendência real:** executar toda validação .NET/PostgreSQL/browser em runner provisionado. Os critérios runtime não foram atingidos neste container.
