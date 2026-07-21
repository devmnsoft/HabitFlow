# Erro PostgreSQL 3D000: banco não existe

Quando o PostgreSQL retorna `3D000`, a aplicação não conseguiu abrir conexão porque o banco configurado ainda não existe.

## Correção local

```powershell
powershell -ExecutionPolicy Bypass -File scripts/database/create-habitflow-db.ps1
powershell -ExecutionPolicy Bypass -File scripts/database/apply-script-completo.ps1 -DevSeed
powershell -ExecutionPolicy Bypass -File scripts/database/validate-db.ps1
```

Credencial de desenvolvimento após seed: `admin@habitflow.local` / `Admin@123`.

## Diagnóstico

Acesse `/health/db`. O endpoint não expõe senha nem connection string completa e retorna `unhealthy` com orientação amigável se o banco estiver ausente.
