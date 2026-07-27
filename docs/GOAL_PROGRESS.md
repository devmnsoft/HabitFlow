# Objetivos e progresso

Tipos aceitos são `HabitCompletions`, `ActiveDays`, `StreakDays`, `WeeklyCompletions` e `Custom`; valores devem ser positivos e o término não pode anteceder o início. Criação e edição sempre usam `client_id` e `user_id`. A migration 043 duplica `client_id` no vínculo para defesa em profundidade e mantém a unicidade de objetivo/hábito. Remover um vínculo nunca remove o hábito.

Toda mudança calculada ou manual deve gerar `goal_progress_events`. Ao alcançar a meta, o objetivo deve passar a `Completed`, preencher `completed_at` uma única vez e disparar notificação/evento de produto de modo idempotente.
