# Migração de dados Firebase

A migração automática depende de exportação controlada do Firestore. Não incluir credenciais Firebase Admin no repositório.

## Mapeamento inicial
- `users/{uid}/profile/main` -> `habitflow.users`
- `users/{uid}/habits` -> `habitflow.habits`
- `completedDates[]` -> `habitflow.habit_completions`
- `supportTickets` -> `habitflow.support_tickets`

A pasta `tools/firebase-migration/` contém placeholders para um importador futuro baseado em JSON exportado.
