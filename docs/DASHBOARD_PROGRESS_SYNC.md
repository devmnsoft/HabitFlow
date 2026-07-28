# Sincronização das métricas

`HabitOccurrenceService` é a regra compartilhada de ocorrências e `ConsistencyService` é a regra canônica de sequência. O calendário usa ambos e o dia local de `UserTimeZoneService`. Dashboard, relatórios e objetivos devem consumir estes serviços ao evoluírem, sem reimplementar denominador ou streak; a sincronização transacional após concluir/desfazer permanece registrada no backlog técnico desta estabilização.
