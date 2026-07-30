# Relatórios client-aware

Os novos overloads semanal, mensal, persistência e CSV exigem `clientId` e `userId`. A fonte canônica é `ProgressSnapshotService.BuildPeriodAsync`. A semana começa na segunda-feira. CSV usa BOM UTF-8, `;`, datas `dd/MM/yyyy`, números pt-BR e neutralização dos prefixos `=`, `+`, `-` e `@`. O nome público é `habitflow-relatorio-AAAA-MM.csv`. Overloads legados foram marcados obsoletos e não devem ser usados por controllers.
