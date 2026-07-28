# Contratos de progresso

Todos os contratos públicos usam exclusivamente `HabitFlow.Application`. ViewModels ficam em `DTOs/Progress`, Row DTOs em `ProgressCalendarRows.cs` e `IProgressCalendarRepository` em `Abstractions/Progress`. Infrastructure depende de Application e implementa o contrato com filtros simultâneos de `client_id` e `user_id`; Web não duplica modelos.
