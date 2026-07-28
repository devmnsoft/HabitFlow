# Auditoria inicial — v6.6

Data: 28 de julho de 2026 (UTC).

## Ponto de partida

- Commit inicial: `0a3ace1` (`ajuste`).
- Branch inicial encontrada: `work`, sem alterações locais. A implementação foi iniciada em
  `feature/activation-library-upgrade-billing-v66`.
- A correção estrutural de `NavigationItem` está presente: o record recebe `Code`, `Label`,
  `Description`, `Icon`, `Url`, `Context`, `RequiredPermission`, `RequiredFeature`, `SortOrder` e
  `IsActive` de forma explícita.

## Banco e migrations

- Foram encontrados 45 arquivos em `database/migrations`.
- O maior prefixo inspecionado é `045` (`045_pwa_product_events_hardening.sql`).
- Nenhum número foi reservado e nenhuma migration foi criada nesta alteração.
- `database/migrate.sql` permanece a entrada canônica para bancos existentes. Os scripts agregados
  não foram executados.

## Estado funcional observado

- Existem rotas MVC para Home, autenticação, hábitos, objetivos, biblioteca, planos, cobrança,
  conta, administração e plataforma.
- A navegação v6.5 já centralizava URLs, permissões e `RequiredFeature`, mas o método síncrono apenas
  filtrava permissões; não consultava o acesso efetivo ao recurso.
- O shell carregava `site.css` e os quatro estilos contextuais ao mesmo tempo. A correção desta
  entrega mantém os estilos compartilhados e seleciona apenas um arquivo contextual por página;
  `site.css` permanece no repositório como legado, mas deixou de ser carregado pelo shell.
- A biblioteca guiada, cobrança por conta, metas, relatórios, PWA e rotinas compartilhadas possuem
  implementações anteriores, mas esta auditoria não as declara completas contra todos os critérios
  verticais da v6.6.
- Foram encontrados 21 arquivos de teste C# antes desta alteração.

## Comandos executados

| Comando | Resultado observado |
| --- | --- |
| `git status --short --branch` | Sucesso; branch `work` limpa no início. |
| `git log -10 --oneline` | Sucesso; commit inicial confirmado. |
| `dotnet --info` | Não executado: binário `dotnet` indisponível. |
| `dotnet clean` | Não executado: binário `dotnet` indisponível. |
| `dotnet restore` | Não executado: binário `dotnet` indisponível. |
| `dotnet build -c Release` | Não executado: binário `dotnet` indisponível. |
| `dotnet test -c Release` | Não executado: binário `dotnet` indisponível. |
| `dotnet format --verify-no-changes` | Não executado: binário `dotnet` indisponível. |
| `psql --version` | Não executado: binário `psql` indisponível. |
| `find database/migrations -maxdepth 1 -type f` | Sucesso; sequência inspecionada até 045. |
| `rg` para `NavigationItem`, `RequiredFeature` e rotas | Sucesso. |

## Limitações e próximos controles

O contêiner não contém o SDK .NET nem o cliente PostgreSQL. Portanto, build, testes, format,
migrations, validação PostgreSQL, Playwright e evidências visuais não foram comprovados nesta
execução. Esses controles devem rodar no CI equipado antes do merge. Nenhuma credencial, screenshot,
binário, diretório `bin`, `obj` ou `publish` foi produzido.
