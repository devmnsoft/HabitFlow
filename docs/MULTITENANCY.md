# Multitenancy HabitFlow v5.9

O HabitFlow usa isolamento lógico por `client_id` no schema explícito `habitflow`. Usuários comuns e Admins carregam o cliente atual a partir do claim `client_id`; SuperAdmin pode operar sem filtro obrigatório em serviços globais.

## Regras
- User acessa apenas dados do próprio `client_id`.
- Admin administra apenas usuários, billing e dados do próprio `client_id`.
- SuperAdmin pode consultar visão global.
- Usuário sem `client_id` deve ficar restrito a onboarding, aceite de convite ou vinculação.
