# Migrations PostgreSQL — v6.13.7

**Status: bloqueado pelo ambiente, não aprovado.** `pwsh`, `psql` e o servidor PostgreSQL não estão instalados. `pg_config --version` informa 16.13, mas isso não constitui um runtime PostgreSQL. A instalação via APT foi tentada e bloqueada pelo proxy com HTTP 403.

O script foi atualizado para emitir o artifact v6.13.7 correto. Não foram executados banco limpo, banco existente, rerun, `schema_migrations` nem as nove consultas de sanidade. Todos esses critérios seguem P0.
