# Backup e Recuperação — HabitFlow v2.1-SecurityOps

## Estratégia recomendada
- Usar exportação nativa do Cloud Firestore para um bucket Google Cloud Storage dedicado, com IAM restrito e versionamento habilitado.
- Agendar exportações diárias via Cloud Scheduler + Cloud Functions/Cloud Run ou comando `gcloud firestore export gs://BUCKET/backups/YYYY-MM-DD`.
- Manter retenção mínima de 30 dias para backups diários e 12 meses para marcos mensais.

## Teste de restauração
1. Restaurar sempre primeiro em projeto Firebase separado de homologação.
2. Validar coleções `users`, `supportTickets`, `systemAuditLogs`, `billingEvents` e `securityIncidents`.
3. Conferir regras, índices e Functions antes de qualquer restauração parcial em produção.

## Restaurar coleção
- Preferir importação seletiva para ambiente de staging.
- Exportar novamente dados atuais antes de restaurar.
- Registrar incidente operacional e janela de manutenção.

## LGPD
- Exportação do titular: solicitar por `requestUserDataExport`, validar identidade e gerar pacote mínimo com dados do próprio usuário.
- Exclusão do titular: solicitar por `requestUserDataDeletion`, preservar registros necessários por obrigação legal/auditoria e anonimizar quando aplicável.
- Nunca enviar dados pessoais por canais inseguros.

## Operação
- Último backup pode ser refletido manualmente em `LAST_BACKUP_AT` nas Functions.
- Incidentes de restauração devem ser documentados em `securityIncidents`.
