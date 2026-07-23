# Tenant isolation

`CurrentUserContext` expõe identidade, papel e `ClientId`. `CurrentTenantService` centraliza `RequireCurrentClientId`, `CanAccessClient` e `EnsureCanAccessClient`.

Toda query de dados de cliente deve incluir `client_id`, exceto consultas explicitamente SuperAdmin.
