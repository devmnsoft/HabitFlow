# Consistência das métricas

Ocorrência, percentual e sequência devem derivar do snapshot canônico. Dias sem agenda não incrementam nem quebram sequência; o dia local atual incompleto não quebra antecipadamente. Semana de produto começa na segunda-feira (ISO-8601).

Dashboard, calendário e relatório devem comparar os mesmos pares `(habitId, localDate)` sob o mesmo `clientId` e `userId`.
