# Inventário v6.12.8

- **SHA inicial:** `3eaf23a3bf87dcd65c985502092aa6a96c9b3102`.
- **PR anterior detectado:** PR #130 no HEAD (`Merge pull request #130`), que já incorporava a primeira entrega v6.12.8 após o PR #129.
- **Branch de trabalho:** `feature/v6128-dashboard-goals-reminders-reports-premium-ux`.
- **Telas priorizadas:** `/dashboard`, `/goals`, `/goals/{id}`, `/reminders`, `/notifications` e `/reports`; biblioteca foi inventariada e preservada por já possuir filtros/templates funcionais.
- **Arquivos principais:** `DashboardOverviewService.cs`, `GoalExperienceServices.cs`, views/partials de Dashboard, Goals, Reminders e Reports, além dos estilos específicos existentes.
- **Ambiente:** SDK .NET indisponível (`dotnet: command not found`), impedindo build, publish e execução autenticada. PostgreSQL, navegador autenticado e dados de usuário não foram disponibilizados. Node/npm estão disponíveis.
