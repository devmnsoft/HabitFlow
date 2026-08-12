# Auditoria de runtime CRUD — v6.12.3

| Rota/método | Action / service / repository | Esperado e estado após correção | Falha/causa/correção | Teste |
|---|---|---|---|---|
| `/habits/create` POST | `HabitsController.Create` / `HabitEditorService.SaveAsync` / `HabitRepository.CreateAsync` | Criar e redirecionar ao detalhe, com agenda persistida atomicamente. | 23502: parâmetro `StartDate` nulo ignorava o default SQL; normalizado pela data local antes do INSERT. | `HabitRepositoryContractTests`; spec hábitos/objetivos |
| `/habits/{id}/edit` POST | `Edit` / `SaveAsync` / `UpdateAsync` | Editar sem consumir cota e preservar `StartDate`. | Registro legado podia continuar nulo; fallback e transação corrigem o contrato. | spec hábitos/objetivos |
| `/habits/{id}/{pause,resume,archive,restore}` POST | `Lifecycle` / `HabitLifecycleService` / `UpdateAsync` | Alterar status com escopo tenant/user. | Predicados conferidos; arquivamento é a exclusão segura. | `CrudSmokeRepositoryTests`; spec hábitos/objetivos |
| `/habits/{id}/{complete,undo-completion}` POST | `Complete`, `Undo` / use cases transacionais | Persistir/desfazer conclusão sem 500. | Fluxo já usa UoW e escopo; mantido. | spec hábitos/objetivos |
| `/goals/{id}` GET | `GoalsController.Detail` / query service / `ListLinkedHabitsAsync` | Abrir detalhe e listar hábitos. | Dapper recebia `h.*`, inclusive coluna legada; projeção canônica corrigida. | `UserGoalRepositoryContractTests` |
| `/goals/{id}/{link-habit,unlink-habit}` POST | `GoalsController` / `GoalService` / `UserGoalRepository` | Vincular somente recursos do mesmo tenant/user. | JOIN/predicados e teste de regressão conferidos. | `CrudSmokeRepositoryTests`; spec hábitos/objetivos |
| `/reminders/*` POST | `RemindersController` / `HabitReminderService` / reminder repository | Criar, pausar, reativar, adiar e excluir com escopo. | Sem mudança de contrato confirmada nesta versão. | spec lembretes/notificações |
| `/notifications/*` POST | `NotificationsController` / notification service/repository | Ler e arquivar com feedback. | Sem mudança de contrato confirmada nesta versão. | spec lembretes/notificações |
| `/account/privacy/*` POST | `AccountPrivacyController` / `LgpdService` / `LgpdRepository` | Consentir e criar pedidos auditáveis, sem exclusão imediata. | Contrato existente preservado. | suíte de privacidade existente |

O resultado efetivo de cada execução está registrado pelos comandos da entrega. Onde PostgreSQL/autenticação não estiver disponível, o item permanece **não validado em runtime**, e não é apresentado como aprovado.
