# TENANT_QUERY_AUDIT_V6_1

## Regra
Repositórios operacionais de cliente devem filtrar por `client_id`; repositórios globais SuperAdmin são explicitamente separados.

| Repository | Método | client_id | Risco | Correção |
|---|---|---:|---|---|
| ClientCommunicationRepository | ListByClientAsync/MarkAsReadAsync | Sim | Baixo | Mantido filtro obrigatório. |
| ClientCommunicationRepository | ListAllAsync | Global SuperAdmin | Médio | Uso restrito ao SuperAdminController. |
| SuperAdminOperationalRepository | List* | Global SuperAdmin | Médio | Repository dedicado a SuperAdmin. |
| SuperAdminOperationalRepository | ações de invoice/subscription/client | Sim/target id | Médio | Atualizações registram auditoria e alteram cliente relacionado. |
| ClientOnboardingRepository | GetOrCreate/UpdateStep | Sim | Baixo | Mantido filtro por client_id. |
