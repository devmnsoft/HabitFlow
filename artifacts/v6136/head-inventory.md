# Inventário do HEAD — v6.13.6

> Data: 2026-08-17 UTC
> SHA inicial: `ee66750e8bac3a3df199e33df49af6ae5a3f958c`

## PRs recentes detectados
- #140/#139: correções de materialização Dapper de `HabitTemplate`; `HabitTemplateProjection` e `HabitTemplateRow` presentes.
- #138: revisão semanal acionável e rotina adaptativa.
- #136: hotfix do partial `_HabitStatusBadge` e materialização de lembretes.

## Contratos conferidos
- Os repositórios de templates e favoritos consultam `HabitTemplateRow` e mapeiam pela projeção; não há `QueryAsync<HabitTemplate>`.

## Módulos e rotas críticas existentes
Autenticação, onboarding, biblioteca/templates/favoritos, hábitos, Meu Dia, Dashboard, objetivos, lembretes, notificações, revisão semanal, relatórios e planos possuem controllers/rotas no código. Rotas prioritárias: `/register`, `/login`, `/onboarding`, `/habit-library`, `/habits`, `/my-day`, `/dashboard`, `/goals`, `/reminders`, `/notifications`, `/weekly-review`, `/reports`, `/plans` e `/account/plan/usage`.

## Arquivos críticos
`Program.cs`, controllers da jornada, repositórios Dapper de templates/lembretes, migrations 052–055, views e JavaScript das telas principais.

## Pendências reais
A imagem de execução não contém .NET, PowerShell, PostgreSQL nem navegador; build, publish, migrations, runtime autenticado e inspeção visual permanecem bloqueados neste ambiente.
