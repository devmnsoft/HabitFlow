# Motor de progresso de objetivos

Tipos são centralizados em `GoalTargetType`: conclusões, dias ativos, sequência, conclusões semanais e customizado. O cálculo automático deve sempre partir de `ProgressPeriodSnapshot`; sequência deve ser fornecida por `ConsistencyService`. Objetivos customizados aceitam apenas alteração manual justificada. Conclusões são históricas: após undo, valor corrente é recalculado, mas `Completed` e `completed_at` permanecem, sem repetir evento ou notificação.

A integração transacional completa do motor não pôde ser validada neste ambiente e permanece pendente de teste.
