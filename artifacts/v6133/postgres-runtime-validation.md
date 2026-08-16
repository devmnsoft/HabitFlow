# Validação PostgreSQL runtime — v6.13.3

**Status: bloqueada, não aprovada.** `pwsh`, `psql` e servidor PostgreSQL não estão instalados. A tentativa de instalá-los via APT foi bloqueada pelo proxy HTTP 403.

O runner foi atualizado para usar banco temporário/relatório v6133, exigir escopo não nulo em `habit_reminders` e incluir `habit_reminders`/`habit_templates` entre as tabelas obrigatórias. Nenhuma migration ou consulta SQL foi declarada executada.
