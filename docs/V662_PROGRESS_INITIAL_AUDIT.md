# Auditoria inicial v6.6.2

- Commit inicial: `ddd6b0b`.
- Branch de trabalho: `fix/progress-calendar-real-metrics-v662`.
- Estado observado: `/progress` executava `Index` e renderizava `ProgressDto(0,0,0,0,0)`; `/progress/calendar`, embora presente no `NavigationService`, não tinha action e resultava em rota não encontrada.
- View encontrada: `Views/Progress/Index.cshtml`, com percentuais e hábitos demonstrativos fixos. Não havia `Calendar.cshtml`.
- Serviços reutilizáveis: `HabitScheduleService`, repositórios de hábitos, dias e conclusões e handlers de `DateOnly`/`TimeOnly`. `ProgressService` calculava streak por datas, mas mantinha taxas fixas; `ReportService` usava hábitos ativos × dias.
- Não foi levantada aplicação HTTP antes da mudança porque o SDK .NET não está instalado no ambiente. Portanto, os códigos anteriores acima resultam da inspeção de roteamento, não de smoke test executado.
- `dotnet --info`, clean, restore, build, test e format: não executáveis (`dotnet: command not found`).
- `psql --version`: não executável (`psql: command not found`).
- Schema existente contém hábitos, `habit_week_days`, conclusões, criação e arquivamento; nenhuma migration é necessária.
