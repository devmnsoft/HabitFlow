# Inventário v6.13.4

- **SHA inicial:** `17c1b49e7b499dfcb64e18e5de66dfbc4812c305`
- **PR anterior:** merge do PR #137 (`17c1b49`), após o PR #136 com os hotfixes `_HabitStatusBadge` e materialização de `HabitReminderRow`/Dapper.
- **Validações anteriores detectadas:** commits v6.13.1 (`f49925d`), v6.13.2 (`f7103f6`) e v6.13.3 (`109893e`).
- **Funcionalidades existentes:** hábitos, objetivos, conclusões, lembretes, notificações, Meu Dia, Dashboard, relatórios, biblioteca, onboarding, planos, busca, agenda efetiva e revisão semanal básica.
- **Telas críticas:** `/weekly-review`, `/habits/{id}`, `/dashboard`, `/my-day`, `/goals/{id}`, `/reminders`, `/reports` e `/account/plan/usage`.
- **Pendências técnicas iniciais:** SDK .NET indisponível no contêiner; validação HTTP exige aplicação e PostgreSQL executáveis.
- **Módulos alterados:** revisão semanal, recomendações determinísticas, ajuste adaptativo de frequência/duração, auditoria e interface de detalhe do hábito.
