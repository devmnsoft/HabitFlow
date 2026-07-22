# Dapper DateOnly/TimeOnly

A v5.7 registra handlers globais para `DateOnly` e `TimeOnly` antes do uso dos repositories. `DateOnly` é enviado como `DbType.Date` e `TimeOnly` como `DbType.Time`, mantendo compatibilidade com PostgreSQL `date` e `time` via Dapper/Npgsql.

Se um log citar `Dapper não reconheceu DateOnly/TimeOnly`, confirme que `DapperTypeHandlers.Register()` é chamado no startup de infraestrutura.
