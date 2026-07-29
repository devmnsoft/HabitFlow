# Snapshot canônico de progresso

`ProgressSnapshotService` consulta dados já isolados por `clientId` e `userId` através de `IProgressCalendarRepository`. A agenda é produzida exclusivamente por `HabitOccurrenceService`; sequências são produzidas exclusivamente por `ConsistencyService`.

Os contratos de dia, período, hábito e sequência são reutilizáveis por Dashboard, calendário e relatórios. O período carregado é o solicitado, limitado pela janela efetiva do plano; não existe janela fixa de dois anos.

Consultas esperadas: uma leitura agregada de progresso e uma leitura de entitlement por snapshot. Não há consulta por hábito nem por dia.
