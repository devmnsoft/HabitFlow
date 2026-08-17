# Objetivos e vínculos — v6.13.6

> Data: 2026-08-17 UTC
> SHA inicial: `ee66750e8bac3a3df199e33df49af6ae5a3f958c`

## Cobertura estática
Rotas autenticadas contemplam CRUD, pausar/retomar/concluir/cancelar e vincular/desvincular hábito. As ações obtêm o tenant pelo contexto atual e detalhes consultam por cliente e usuário.

## Runtime
Timeline, progresso, estados vazios, vínculo real e isolamento em banco: **pendentes** sem runtime/PostgreSQL.
