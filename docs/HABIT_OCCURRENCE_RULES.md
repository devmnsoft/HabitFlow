# Regras de ocorrência

`HabitOccurrenceService` é a regra canônica: Daily ocorre diariamente; Weekdays de segunda a sexta; Weekends sábado e domingo; CustomWeekly apenas nos dias de `habit_week_days`. Nunca há ocorrência antes da criação nem depois do dia local do arquivamento. O calendário pode exibir previsões futuras, mas métricas históricas as excluem. Consultas são em lote, sem query por hábito ou dia.
