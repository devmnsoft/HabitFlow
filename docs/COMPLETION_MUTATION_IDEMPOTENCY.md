# Idempotência da mutação de conclusão

`CompletionMutationResult` torna explícitos `Created`, `Deleted`, `FinalState`, data local e identificador. A inserção usa uma única instrução `INSERT … ON CONFLICT DO NOTHING RETURNING id`, apoiada na unicidade `(habit_id, completed_date)`. A remoção usa `DELETE … RETURNING id`. Ambas filtram conta, pessoa e hábito; somente uma mutação real dispara auditoria. Replays retornam o estado final sem repetir efeitos.
