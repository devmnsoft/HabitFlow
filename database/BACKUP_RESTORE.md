# Diretrizes de Backup, Restore e Disaster Recovery — HabitFlow SaaS (v6.26.0)

Este documento estabelece a governança operacional, arquitetura, automação e procedimentos de Disaster Recovery para os bancos de dados PostgreSQL do HabitFlow SaaS.

---

## 1. Métricas de Resiliência (RPO e RTO)

| Métrica | Definição | Alvo de Produção |
|---|---|---|
| **RPO (Recovery Point Objective)** | Janela máxima tolerada de perda potencial de dados em caso de falha catastrófica. | **≤ 1 hora** (com backups incrementais e WAL archiving) |
| **RTO (Recovery Time Objective)** | Tempo máximo aceitável para restaurar e validar a integridade da aplicação em produção. | **≤ 30 minutos** (em ambiente de contingência ou repositório limpo) |

---

## 2. Estratégia de Backup

1. **Backup Diário Completo (Full Dump):**
   - Executado em janela de menor tráfego (02:00 UTC).
   - Formato personalizado comprimido do PostgreSQL (`-Fc`) contendo esquema `habitflow` e metadados.
   - Geração automática de checksum SHA-256 e registro em `habitflow.backup_records`.
2. **Backups Contínuos (WAL Archiving):**
   - Habilitado no PostgreSQL em produção (`wal_level = replica`, `archive_mode = on`).
   - Garante recuperação ponto-no-tempo (Point-in-Time Recovery - PITR).
3. **Imutabilidade e Retenção:**
   - Backups diários: retidos por 30 dias.
   - Backups semanais: retidos por 90 dias.
   - Backups mensais (fechamento fiscal/LGPD): retidos por 5 anos em storage com WORM (Write Once, Read Many).

---

## 3. Scripts de Automação Segura

### 3.1 Script de Backup (`scripts/backup/backup-database.ps1`)
- Lê variáveis de ambiente seguras (`DATABASE_URL` ou parâmetros explícitos).
- Nunca grava senhas em texto puro nem as imprime em logs ou terminais.
- Gera arquivo `habitflow_backup_YYYYMMDD_HHMMSS.dump`.
- Calcula o hash SHA-256 e salva em `.sha256`.

### 3.2 Script de Verificação de Integridade (`scripts/backup/verify-backup.ps1`)
- Valida o hash SHA-256 em relação ao arquivo de dump.
- Executa `pg_restore --list` no arquivo para validar a estrutura interna dos blocos sem tocar no banco de dados ativo.

---

## 4. Checklist de Restore em Ambiente Descartável (Disposable Recovery)

> [!CAUTION]
> **NUNCA** execute um comando de restore sobre o banco de dados de produção ativo sem parada programada, confirmação prévia e aprovação formal do SuperAdmin.

Passo a passo para homologação em ambiente descartável:

1. **Subir instância temporária (Docker ou VM descartável):**
   ```bash
   docker run --name habitflow-restore-test -e POSTGRES_PASSWORD=restoretest -p 5433:5432 -d postgres:16-alpine
   ```
2. **Criar banco e extensões:**
   ```bash
   docker exec -i habitflow-restore-test psql -U postgres -c "CREATE DATABASE habitflow_test;"
   docker exec -i habitflow-restore-test psql -U postgres -d habitflow_test -c "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";"
   ```
3. **Executar restauração do dump validado:**
   ```bash
   pg_restore -h localhost -p 5433 -U postgres -d habitflow_test -v habitflow_backup.dump
   ```
4. **Verificar integridade do esquema e registros:**
   - Conferir se todas as migrations estão registradas em `habitflow.__schema_migrations`.
   - Executar query de sanity check:
     ```sql
     SELECT COUNT(*) FROM habitflow.clients;
     SELECT COUNT(*) FROM habitflow.users;
     SELECT COUNT(*) FROM habitflow.habits;
     SELECT COUNT(*) FROM habitflow.system_health_checks;
     ```
5. **Destruir o ambiente de teste:**
   ```bash
   docker stop habitflow-restore-test && docker rm habitflow-restore-test
   ```
6. **Registrar o teste com êxito na tabela de governança:**
   ```sql
   UPDATE habitflow.backup_records
   SET status = 'Verified', integrity_status = 'Valid', verified_at = CURRENT_TIMESTAMP
   WHERE file_name = 'habitflow_backup.dump';
   ```

---

## 5. LGPD e Retenção no Restore

- Backups antigos não devem ser utilizados para sobrescrever dados de usuários que exerceram seu direito de anonimização ou exclusão sob a LGPD.
- Em caso de Disaster Recovery de um backup anterior à exclusão, o processo automatizado de reprocessamento deve consultar a tabela `habitflow.lgpd_requests` (com status `Processada`) e reaplicar as exclusões e anonimizações pendentes antes da reabertura do tráfego público.
