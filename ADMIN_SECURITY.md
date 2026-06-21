# Segurança do Admin Geral

## Fase atual
Functions validam admin no backend por `ADMIN_EMAILS` configurado no ambiente das Functions. O frontend pode esconder abas, mas isso não é segurança.

## Fase recomendada
Migrar para Firebase Auth custom claims: `{ generalAdmin: true }`.

Funções/scripts futuros:
- `setAdminClaim(uid)`
- `revokeAdminClaim(uid)`

Nunca expor regras administrativas, tokens ou listas sensíveis no frontend.
