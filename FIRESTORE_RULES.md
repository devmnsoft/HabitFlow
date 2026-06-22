
## v2.3-AdminGlobal

As regras bloqueiam leitura/escrita direta de `adminUserNotes`, `adminAuditLogs`, `systemAuditLogs` e `billingEvents`. Campos administrativos do perfil (`plan`, `planStatus`, `accountStatus`, `riskStatus` e contadores operacionais) não podem ser alterados pelo próprio usuário.
