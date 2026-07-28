# Contratos Dapper de progresso

Consultas usam o schema `habitflow` e aliases correspondentes aos Row DTOs. `completed_date` materializa como `DateOnly`, `reminder_time` como `TimeOnly?`, timestamps como `DateTime`, e `frequency_type` varchar como `FrequencyTypeCode`. Somente Daily, Weekdays, Weekends e CustomWeekly são mapeados; código desconhecido gera warning e nenhuma ocorrência, sem derrubar a página.
