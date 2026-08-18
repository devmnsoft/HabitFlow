# Jornada MVP: biblioteca até hábito

## Revisão estática

1. Login e onboarding possuem rotas no smoke autenticado.
2. Biblioteca lista somente templates ativos e publicados; favoritos são segregados por cliente e usuário.
3. Detalhe rejeita template não publicado.
4. O GET de customização apenas monta o view model; não chama criação.
5. A criação ocorre exclusivamente no POST antiforgery, por `CreateHabitFromTemplateUseCase`.
6. Dashboard, Meu Dia, hábitos, revisão semanal, relatórios e uso do plano fazem parte do smoke autenticado.

## Estado runtime

**Pendente:** sem SDK .NET, PostgreSQL e usuário autenticado neste container, não foi possível executar template → customização → criação → conclusão → streak/progresso. O runner Windows registra essa jornada como pendência manual em vez de fabricar aprovação.
