# HabitFlow Onboarding, Rotinas e Retencao - Auditoria Incremental

Data: 2026-10-10

## Cobertura revisada

Arquivos e fluxos lidos nesta evolucao:

- `OnboardingController`, `OnboardingJourneyService`, `PersonalOnboardingJourneyService` e repositórios de onboarding.
- Views `Onboarding/Index`, `Onboarding/Templates` e `Onboarding/Complete`.
- `DailyRoutinePlannerService`, `MyDayController`, `WeeklyReviewService` e `WeeklyReviewController`.
- Entidades `UserOnboardingProgress`, `RoutinePlanning`, `Habit` e `ProgressHabitRow`.
- Migrations `052`, `053`, `054`, `055`, `087`, `094`, `096`, `102` e `103`.
- Testes de onboarding, jornada adaptativa e criacao guiada.

## Estado dos itens pedidos

| Item | Classificacao | Evidencia |
| --- | --- | --- |
| Navegacao por perfil e plano | Funcional com evidencia | `NavigationService.Definitions`, `NavigationAccessEvaluator`, `docs/NAVIGATION_ACCESS_MATRIX.md`. |
| Criacao guiada de habitos | Funcional com evidencia | `HabitsController`, partials de `Habits`, `habits-v4.js`, `DailyExperienceGuidedCreationTests`. |
| Biblioteca e rotinas | Parcial | Biblioteca funcional em `HabitLibraryService`; rotinas existem como jornada diaria e colecoes/templates, mas ainda nao ha entidade completa de sessao de rotina nomeada. |
| Calendario | Funcional com evidencia | `ProgressCalendarService`, `HabitOccurrenceService`, `DailyRoutinePlannerService`. |
| Lembretes | Funcional com evidencia | `HabitReminderService`, `ReminderDispatchProcessor`, migrations `038`, `067`, `068`. |
| IA contextual | Parcial | Multi-provedor e admin existem em `AiAssistantProviders`, `AiAdminService`; aplicacao de propostas continua manual/confirmada. |
| Relatorios | Funcional com evidencia | `ReportService`, `ReportsController`, `WeeklyReviewService`. |
| Idiomas | Parcial | Há textos em PT nas jornadas revisadas; nao foi encontrada localizacao completa PT/EN/ES/FR para essas telas. |
| Trial e cobranca | Funcional com evidencia | `BillingService`, `PlanEntitlementService`, migration `096`, views de planos/billing. Homologacao real de pagamento depende de credenciais externas. |

## Achados e correcoes aplicadas

| Arquivo | Simbolo | Impacto | Correcao |
| --- | --- | --- | --- |
| `UserOnboardingProgressRepository` | `StartOrRestartAsync` | Um onboarding concluido podia ser reiniciado por chamada acidental. | O `upsert` agora preserva registros com `completed_at` preenchido. |
| `OnboardingController` | `Index`, `Templates` | Usuario com onboarding concluido podia rever telas iniciais. | Fluxo concluido redireciona para `MyDay` com mensagem. |
| `OnboardingController` | `AddTemplate`, `CompletePost` | Primeiro habito podia ser criado sem marcar onboarding como concluido. | Fluxo chama `CompleteAsync`, limpa drafts e preserva versao. |
| `Onboarding/Templates.cshtml` | formulario `skip` | Pular na tela de templates podia nao persistir por falta de versao. | Campo oculto `version` adicionado. |
| `DailyRoutinePlannerService` | `ToProgressRow` | Meu Dia podia ignorar inicio/fim, pausa e metadados adaptativos. | Mapper alinhado com `HabitCompletionUseCases`. |
| `WeeklyReviewService` | `ToProgressRow` | Revisao semanal podia contar ocorrencias fora da vigencia do habito. | Mapper alinhado com datas, pausa e campos quantitativos. |

## Pendencias externas ou maiores

- Sessao de rotina nomeada com iniciar, pausar, retomar e encerrar exige modelagem adicional; o incremento atual estabiliza a rotina diaria existente.
- Localizacao completa em quatro idiomas requer inventario de infraestrutura i18n e extração de textos das views.
- Homologacao de Groq, Gemini, DeepSeek e Mercado Pago depende de secrets/configuracao externos e nao deve ser simulada.
- Playwright mobile/desktop existe no repo, mas esta entrega foi validada por build/testes de fonte e comandos do projeto; QA visual automatizado completo deve rodar em ambiente com navegador configurado.
