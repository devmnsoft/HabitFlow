# Consistência entre períodos

A sequência percorre dias previstos ordenados, atravessa meses e anos e ignora dias sem agenda. Dia previsto encerrado e incompleto quebra a sequência; o dia local atual incompleto permanece em andamento e não quebra a sequência até seu encerramento. A melhor sequência é calculada na janela acessível, indicada por `ConsistencyPeriodStart`, `ConsistencyPeriodEnd` e `IsBestStreakLimitedByPlan`.
