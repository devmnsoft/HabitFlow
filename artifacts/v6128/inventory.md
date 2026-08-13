# Inventário v6.12.8

- **SHA inicial:** `74d5a63026ee29e1a003fc2cd428d4e9273c1e23`.
- **PR anterior detectado:** PR #129 (`Merge pull request #129`, evolução de Meu Dia e ações de hábito).
- **Branch de trabalho:** `feature/v6128-dashboard-goals-reminders-reports-premium-ux`.
- **Telas priorizadas:** `/dashboard`, `/goals`, `/goals/{id}`, `/reminders` e `/reports`; notificações e biblioteca foram inventariadas e preservadas por já possuírem inbox/templates funcionais.
- **Arquivos principais:** `DashboardOverviewService.cs`, `GoalExperienceServices.cs`, views/partials de Dashboard, Goals, Reminders e Reports, além dos estilos específicos existentes.
- **Ambiente:** SDK .NET indisponível (`dotnet: command not found`), impedindo build, publish e execução autenticada. PostgreSQL, navegador autenticado e dados de usuário não foram disponibilizados. Node/npm estão disponíveis.
