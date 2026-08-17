# Biblioteca, templates e favoritos — v6.13.6

> Data: 2026-08-17 UTC
> SHA inicial: `ee66750e8bac3a3df199e33df49af6ae5a3f958c`

## Conferido no código
- Rotas de listagem, detalhe, customização, uso e favorito existem.
- `HabitTemplateRepository` e `HabitTemplateFavoriteRepository` usam o DTO `HabitTemplateRow` e `HabitTemplateProjection`; não materializam diretamente a entidade.
- O JavaScript da biblioteca passou em `node --check`.

## Runtime
Filtros, favoritos, gates de plano e round-trip PostgreSQL permanecem pendentes por ausência de .NET/PostgreSQL.
