# v6.8.1 — conclusão transacional e Dashboard

`CompleteHabitUseCase` e `UndoHabitCompletionUseCase` validam tenant, usuário, propriedade, arquivo e data local. A constraint existente `unique(habit_id, completed_date)` e `ON CONFLICT DO NOTHING` tornam conclusão concorrente idempotente; undo repetido também é idempotente.

Conclusão, auditoria e snapshot usam uma Unit of Work. `ProgressSnapshotService` fornece métricas e próximo hábito. O Dashboard e o JSON AJAX consomem o mesmo snapshot, sem constantes. O JavaScript mantém antiforgery, trava clique duplo, anuncia resultado e atualiza texto/KPIs sem `innerHTML` ou reload.
