# Backup do schema HabitFlow

O backup padrão continua sendo do banco inteiro. Para restringir ao schema oficial da aplicação, use:

```powershell
scripts/windows/backup-database.ps1 -HabitflowSchemaOnly
```

Opções úteis:

- `-SchemaOnly`: exporta apenas estrutura.
- `-DataOnly`: exporta apenas dados.
- `-HabitflowSchemaOnly`: adiciona `pg_dump --schema=habitflow`.

Backups e dumps não devem ser commitados.
