# Correções de runtime — v6.13.7

| Erro | Causa | Arquivo | Correção | Validação |
|---|---|---|---|---|
| Relatório PostgreSQL ainda apontava para v6.13.6 | Caminho/título desatualizados | `scripts/validation/validate-postgres-migrations.ps1` | Promovidos banco temporário, caminho e título para v6.13.7 | Revisão de diff; execução bloqueada sem pwsh/psql |
| Smoke omitia rotas críticas, parametrizadas e reload | Cobertura incompleta do helper operacional | `scripts/validation/smoke-authenticated-routes.ps1` | Adicionados favoritos, objetivo, revisão semanal, IDs opcionais, reload e detecção de redirect/exceção | Revisão de diff; execução bloqueada sem pwsh/runtime |

Nenhum bug da aplicação foi alegado sem reprodução real.
