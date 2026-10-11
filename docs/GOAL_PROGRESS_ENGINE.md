# Motor de progresso de objetivos

Tipos são centralizados em `GoalTargetType`: conclusões, dias ativos, sequência, conclusões semanais e customizado. O cálculo automático deve sempre partir de `GoalProgressSnapshot`; sequência deve ser fornecida por `ConsistencyService`. Objetivos customizados aceitam apenas alteração manual justificada.

Política de conclusão: conquistas são históricas. Depois de desfazer uma conclusão de hábito, o valor corrente e o percentual são recalculados, mas uma meta já concluída permanece `Completed` e preserva `completed_at`. O sistema não reabre automaticamente a meta, evitando contradição entre histórico de conquista e ajustes posteriores.

Escopo de dados: snapshots automáticos contam apenas conclusões do mesmo `client_id` e `user_id`. Quando a meta possui vínculos em `goal_habits`, somente hábitos vinculados ao mesmo tenant entram no agregado. Metas sem vínculo explícito mantêm o comportamento legado de agregar hábitos autorizados do usuário no período da meta.

Consistência: semanas são agrupadas por ano ISO e número ISO compatíveis. Uma semana só entra em `ConsistentWeeks` depois do encerramento da semana ISO; a semana corrente não recebe conclusão definitiva enquanto ainda pode haver ocorrências futuras. `ActiveDays` significa dias planejados avaliados, não dias com qualquer atividade.

A integração transacional completa do motor não pôde ser validada neste ambiente e permanece pendente de teste.
