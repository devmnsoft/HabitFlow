# Auditoria de conclusão da v6.3

Auditoria estática realizada antes das alterações da v6.4. A execução .NET não estava disponível no ambiente de trabalho (`dotnet: command not found`), portanto itens sem cobertura executável permanecem explicitamente classificados como **sem teste**.

| Item | Estado encontrado | Tratamento v6.4 |
|---|---|---|
| DashboardController/DashboardService | quebrado: métricas fixas e ausência de serviço dedicado | pendente; não declarado como concluído |
| HabitService/HabitsController | parcial: persistência existe; resposta Ajax contém zeros fixos | pendente |
| HabitCompletionRepository | parcial, requer teste PostgreSQL de isolamento | sem teste |
| GoalService/GoalsController/UserGoalRepository | quebrado: POST de edição apenas redirecionava | **corrigido na v6.4** com validação e persistência escopada |
| FeatureAccessService/PlanEntitlementService | parcial | sem teste de matriz completa |
| ReportService/NotificationService | parcial | sem teste |
| Migration 036 | parcial: objetivos e vínculos, sem `client_id` no vínculo | **corrigido na v6.4** pela migration 043 |
| Migration 037 | apenas estrutura: tabelas de marcos | processador pendente |
| Migration 038 | apenas estrutura: lembretes e preferências | scheduler pendente |
| Migration 039 | apenas estrutura | fluxos compartilhados pendentes |
| Migration 040 | parcial: visibilidade e eventos | índices de analytics/PWA adicionados |
| manifest/service worker/pwa/offline | parcial: proteção de rotas privadas existe; ativação era imediata | endurecido na v6.4 |
| Views mobile | parcial | QA visual manual pendente |
| Testes v6.3 | sem teste executável neste ambiente | requer SDK .NET e PostgreSQL configurado |

A existência de tabela, View ou documentação não foi tratada como prova de funcionalidade operacional.
