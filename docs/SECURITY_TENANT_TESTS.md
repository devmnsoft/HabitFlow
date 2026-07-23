# Testes de segurança tenant

Cobrir obrigatoriamente:
- User A não acessa hábito do Client B.
- Admin A não lista usuários/billing do Client B.
- SuperAdmin acessa ambos.
- Convite do Client A não vincula Client B.
- Relatórios, notificações e suporte filtram `client_id`.
