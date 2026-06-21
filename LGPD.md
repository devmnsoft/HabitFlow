# LGPD Operacional

Coleção: `lgpdRequests/{requestId}`.

Campos: `userId`, `userEmail`, `type`, `status`, `protocol`, `createdAt`, `updatedAt`, `completedAt`, `handledBy`, `notes`, `rejectionReason`.

Functions:
- `createLgpdRequest`
- `getMyLgpdRequests`
- `cancelMyLgpdRequest`
- `getAdminLgpdRequests`
- `updateLgpdRequestStatus`
- `generateUserDataExport`
- `deleteUserDataControlled`

Exclusão exige `confirmationText: "CONFIRMAR_EXCLUSAO"` e deve ser simulada com `dryRun` antes da execução real.
