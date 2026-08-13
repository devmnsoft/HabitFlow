# Validação dos scripts v6.13.2

| Script | Revisão | Resultado |
|---|---|---|
| `validate-local-windows.ps1` | Uso documentado; outputs isolados em v6132; build/publish/security falham imediatamente | Aprovado por revisão; execução bloqueada sem PowerShell/.NET/psql |
| `validate-postgres-migrations.ps1` | Uso documentado; valida identificador do banco temporário; mascara credencial por não registrá-la; fresh/existing/rerun e sanidades preservados | Aprovado por revisão; execução bloqueada sem PowerShell/psql/PostgreSQL |
| `smoke-authenticated-routes.ps1` | Uso documentado; senha `SecureString`; relatório v6132; falha agregada para qualquer rota | Aprovado por revisão; execução bloqueada sem runtime |
| `provision-dev-user.ps1` | Uso documentado; exige Development; senha forte aleatória; não cria SuperAdmin | Aprovado por revisão; execução bloqueada sem runtime |
| `seed-demo-data.ps1` | Uso documentado; override de ambiente removido; exige Development; `ON CONFLICT` mantém idempotência | Aprovado por revisão; execução bloqueada sem psql/PostgreSQL |

A validação sintática nativa ficou **Bloqueada**, pois `pwsh` não está instalado. Não houve registro de connection string, senha ou token.
